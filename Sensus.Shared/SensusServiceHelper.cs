// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xamarin.Forms;
using Newtonsoft.Json;

using Sensus.UI;
using Sensus.Probes;
using Sensus.Context;
using Sensus.UI.Inputs;
using Sensus.Concurrent;
using Sensus.Exceptions;
using Sensus.Probes.Location;
using Sensus.Probes.User.Scripts;
using Sensus.Notifications;

using Plugin.Permissions;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Sensus.Callbacks;
using ZXing;
using ZXing.Net.Mobile.Forms;
using ZXing.Mobile;
using Sensus.Authentication;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Sensus.DataStores.Remote;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Plugin.ContactService.Shared;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace Sensus
{
	/// <summary>
	/// Provides platform-independent functionality.
	/// </summary>
	public abstract class SensusServiceHelper : ISensusServiceHelper
	{
		#region static members
		private static SensusServiceHelper SINGLETON;
		public const int PARTICIPATION_VERIFICATION_TIMEOUT_SECONDS = 60;

		/// <summary>
		/// App Center key for Android app. To obtain this key, create a new Xamarin Android app within the Microsoft App Center. This
		/// is optional. If you do not provide this key, then Sensus will not send Android crash reports and remote health telemetry 
		/// to the App Center.
		/// </summary>
		public const string APP_CENTER_KEY_ANDROID = "";

		/// <summary>
		/// App Center key for iOS app. To obtain this key, create a new Xamarin iOS app within the Microsoft App Center. This
		/// is optional. If you do not provide this key, then Sensus will not send iOS crash reports and remote health telemetry 
		/// to the App Center.
		/// </summary>
		public const string APP_CENTER_KEY_IOS = "";

		/// <summary>
		/// The 64-character hex-encoded string for a 256-bit symmetric AES encryption key. Used to secure protocols for distribution. Can be generated with the following command:
		/// 
		///     openssl enc -aes-256-cbc -k secret -P -md sha1
		/// 
		/// The above was adapted from [this](https://www.ibm.com/support/knowledgecenter/SSLVY3_9.7.0/com.ibm.einstall.doc/topics/t_einstall_GenerateAESkey.html) guide.
		/// 
		/// This is mandatory.
		/// </summary>
		public const string ENCRYPTION_KEY = "";

		/// <summary>
		/// The build ID, used to tag each <see cref="Datum"/>. This is an arbitrary string value, and it is optional.
		/// </summary>
		public const string BUILD_ID = "";

		public static readonly string SHARE_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "share");
		private static readonly string LOG_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_log.txt");
		private static readonly string SERIALIZATION_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sensus_service_helper.json");

#if DEBUG || UI_TESTING
		/// <summary>
		/// The health test interval.
		/// </summary>
		public static readonly TimeSpan HEALTH_TEST_DELAY = TimeSpan.FromSeconds(30);
#elif RELEASE
		/// <summary>
		/// The health test interval.
		/// </summary>
		public static readonly TimeSpan HEALTH_TEST_DELAY = TimeSpan.FromHours(3);
#endif

		public static readonly JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new JsonSerializerSettings
		{
			PreserveReferencesHandling = PreserveReferencesHandling.Objects,
			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			TypeNameHandling = TypeNameHandling.All,
			ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,

			#region need the following in order to deserialize protocols between OSs, which have different probes, etc.
			Error = (o, e) =>
			{
				string errorMessage = "Failed to (de)serialize some part of the JSON:  " + e.ErrorContext.Error + ". Path:  " + e.ErrorContext.Path;

				// may not immediately have a service helper for logging. write to console in such cases.
				if (Get() == null)
				{
					Console.Error.WriteLine(errorMessage);
				}
				else
				{
					Get().Logger.Log(errorMessage, LoggingLevel.Normal, typeof(SensusServiceHelper));
				}

				// don't throw any errors back up, as we might be on the UI thread and crash the app.
				e.ErrorContext.Handled = true;
			},

			// need to ignore missing members for cross-platform deserialization
			MissingMemberHandling = MissingMemberHandling.Ignore,

			// must use indented formatting in order for cross-platform type conversion to work (depends on each "$type" name-value pair being on own line).
			Formatting = Formatting.Indented
			#endregion
		};

		private static event EventHandler OnInitialized;

		public static void WhenInitialized(Action<SensusServiceHelper> onInitialized)
		{
			if (SINGLETON == null)
			{
				OnInitialized += (s, e) => onInitialized(SINGLETON);
			}
			else
			{
				onInitialized(SINGLETON);
			}
		}
		public async static Task WhenInitializedAsync(Func<SensusServiceHelper, Task> onInitialized)
		{
			if (SINGLETON == null)
			{
				OnInitialized += async (s, e) => await onInitialized(SINGLETON);
			}
			else
			{
				await onInitialized(SINGLETON);
			}
		}

		public static HttpClient HttpClient { get; private set; }

		static SensusServiceHelper()
		{
			HttpClient = new HttpClient();
		}

		/// <summary>
		/// Initializes the sensus service helper. Must be called when app first starts, from the main / UI thread.
		/// </summary>
		/// <param name="createNew">Function for creating a new service helper, if one is needed.</param>
		public static void Initialize(Func<SensusServiceHelper> createNew)
		{
			if (SINGLETON != null)
			{
				SINGLETON.Logger.Log("Serivce helper already initialized. Nothing to do.", LoggingLevel.Normal, SINGLETON.GetType());

				return;
			}

			Exception deserializeException;
			if (!TryDeserializeSingleton(out deserializeException))
			{
				// we really couldn't deserialize the service helper! try to create a new service helper...
				try
				{
					SINGLETON = createNew();
				}
				catch (Exception singletonCreationException)
				{
					// report exception and crash app
					string error = "Failed to construct service helper:  " + singletonCreationException.Message + Environment.NewLine + singletonCreationException.StackTrace;
					Console.Error.WriteLine(error);
					Exception exceptionToReport = new Exception(error);
					SensusException.Report(exceptionToReport);
					throw exceptionToReport;
				}

				SINGLETON.Logger.Log("Repeatedly failed to deserialize service helper. Most recent exception:  " + (deserializeException?.Message ?? "No exception"), LoggingLevel.Normal, SINGLETON.GetType());
				SINGLETON.Logger.Log("Created new service helper after failing to deserialize the old one.", LoggingLevel.Normal, SINGLETON.GetType());
			}

			if (OnInitialized != null)
			{
				foreach (EventHandler onInitialized in OnInitialized.GetInvocationList())
				{
					Task.Factory.FromAsync((a, _) => onInitialized.BeginInvoke(SINGLETON, EventArgs.Empty, a, _), onInitialized.EndInvoke, null);
				}
			}
		}

		private static bool TryDeserializeSingleton(out Exception ex)
		{
			ex = null;
			try
			{
				byte[] encryptedJsonBytes;
				try
				{
					encryptedJsonBytes = ReadAllBytes(SERIALIZATION_PATH);
				}
				catch (Exception exception)
				{
					throw new Exception($"Failed to read service helper file into byte array:  {exception.Message}");
				}

				string decryptedJSON;
				try
				{
					// once upon a time, we made the poor decision to encode the helper as unicode (UTF-16). can't switch to UTF-8 now...
					decryptedJSON = SensusContext.Current.SymmetricEncryption.DecryptToString(encryptedJsonBytes, Encoding.Unicode);
				}
				catch (Exception exception)
				{
					throw new Exception($"Failed to decrypt service helper byte array (length={encryptedJsonBytes.Length}) into JSON:  {exception.Message}");
				}

				try
				{
					SINGLETON = JsonConvert.DeserializeObject<SensusServiceHelper>(decryptedJSON, JSON_SERIALIZER_SETTINGS);
				}
				catch (Exception exception)
				{
					throw new Exception($"Failed to deserialize service helper JSON (length={decryptedJSON.Length}) into service helper:  {exception.Message}");
				}
			}
			catch (Exception exception)
			{
				ex = exception;
				Console.Error.WriteLine(exception.Message);
			}

			SINGLETON?.Logger.Log("Deserialized service helper with " + SINGLETON.RegisteredProtocols.Count + " protocols.", LoggingLevel.Normal, SINGLETON.GetType());

			return SINGLETON != null;
		}

		public static SensusServiceHelper Get()
		{
			return SINGLETON;
		}

		/// <summary>
		/// Reads all bytes from a file. There's a File.ReadAllBytes method in Android / iOS, but not in WinPhone.
		/// </summary>
		/// <returns>The bytes.</returns>
		/// <param name="path">Path.</param>
		public static byte[] ReadAllBytes(string path)
		{
			byte[] fileBytes = null;

			using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				fileBytes = new byte[file.Length];
				byte[] blockBytes = new byte[1024];
				int blockBytesRead;
				int totalBytesRead = 0;
				while ((blockBytesRead = file.Read(blockBytes, 0, blockBytes.Length)) > 0)
				{
					Array.Copy(blockBytes, 0, fileBytes, totalBytesRead, blockBytesRead);
					totalBytesRead += blockBytesRead;
				}

				if (totalBytesRead != fileBytes.Length)
				{
					throw new Exception("Mismatch between file length (" + file.Length + ") and bytes read (" + totalBytesRead + ").");
				}
			}

			return fileBytes;
		}

		public static async Task<byte[]> ReadAllBytesAsync(Stream stream)
		{
			const int BUFFER_SIZE = 1024;
			byte[] fileBuffer = new byte[stream.Length];
			byte[] buffer = new byte[BUFFER_SIZE];
			int totalBytesRead = 0;
			int bytesRead = 0;

			while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				Array.Copy(buffer, 0, fileBuffer, totalBytesRead, bytesRead);

				totalBytesRead += bytesRead;
			}

			return fileBuffer;
		}

		public static double GetDirectorySizeMB(string directory)
		{
			double directorySizeMB = 0;

			foreach (string path in Directory.GetFiles(directory))
			{
				directorySizeMB += GetFileSizeMB(path);
			}

			return directorySizeMB;
		}

		public static double GetFileSizeMB(params string[] paths)
		{
			return paths.Sum(path =>
			{
				// files have a habit of racing to deletion...
				try
				{
					return new FileInfo(path).Length / Math.Pow(1024d, 2);
				}
				catch (Exception)
				{
					return 0;
				}
			});
		}

		/// <remarks>
		/// For testing purposes only
		/// </remarks>
		public static void ClearSingleton()
		{
			SINGLETON = null;
		}

		public static async Task<Contact> GetContactAsync(string phoneNumber)
		{
			if (await Get().ObtainPermissionAsync(Permission.Contacts) == PermissionStatus.Granted)
			{
				IList<Contact> contacts = await Plugin.ContactService.CrossContactService.Current.GetContactListAsync();

				return contacts.FirstOrDefault(x => x.Numbers.Select(s => Regex.Replace(s, "[^0-9a-z]", "", RegexOptions.IgnoreCase)).Contains(phoneNumber));
			}

			return null;
		}

		#endregion

		private Logger _logger;
		private Dictionary<string, ProtocolState> _protocolStates;
		private ScheduledCallback _healthTestCallback;
		private SHA256Managed _hasher;
		private List<PointOfInterest> _pointsOfInterest;
		private BarcodeWriter _barcodeWriter;
		private bool _flashNotificationsEnabled;
		private ConcurrentObservableCollection<Protocol> _registeredProtocols;
		private ConcurrentObservableCollection<Script> _scriptsToRun;
		private ConcurrentObservableCollection<UserMessage> _notifications;
		private bool _updatingPushNotificationRegistrations;
		private bool _updatePushNotificationRegistrationsOnNextHealthTest;
		private bool _saving;

		private readonly object _healthTestCallbackLocker = new object();
		private readonly object _shareFileLocker = new object();
		private readonly object _saveLocker = new object();
		private readonly object _updatePushNotificationRegistrationsLocker = new object();

		[JsonIgnore]
		public ILogger Logger
		{
			get { return _logger; }
		}

		public ConcurrentObservableCollection<Protocol> RegisteredProtocols
		{
			get { return _registeredProtocols; }
		}

		public List<string> RunningProtocolIds
		{
			get { return _protocolStates.Where(x => x.Value == ProtocolState.Running).Select(x => x.Key).ToList(); }
		}

		public Dictionary<string, ProtocolState> ProtocolStates
		{
			get
			{
				return _protocolStates;
			}
		}

		public List<PointOfInterest> PointsOfInterest
		{
			get { return _pointsOfInterest; }
		}

		[JsonIgnore]
		public BarcodeWriter BarcodeWriter
		{
			get
			{
				return _barcodeWriter;
			}
		}

		public bool FlashNotificationsEnabled
		{
			get
			{
				return _flashNotificationsEnabled;
			}
			set
			{
				_flashNotificationsEnabled = value;
			}
		}

		public ConcurrentObservableCollection<Script> ScriptsToRun
		{
			get
			{
				return _scriptsToRun;
			}
		}

		public ConcurrentObservableCollection<UserMessage> UserMessages
		{
			get
			{
				return _notifications;

			}
		}

		[JsonIgnore]
		public abstract string PushNotificationToken { get; }

		[JsonIgnore]
		public float GpsDesiredAccuracyMeters
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? Protocol.GPS_DEFAULT_ACCURACY_METERS : runningProtocols.Min(p => p.GpsDesiredAccuracyMeters);
			}
		}

		[JsonIgnore]
		public int GpsMinTimeDelayMS
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? Protocol.GPS_DEFAULT_MIN_TIME_DELAY_MS : runningProtocols.Min(p => p.GpsMinTimeDelayMS);
			}
		}

		[JsonIgnore]
		public float GpsMinDistanceDelayMeters
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? Protocol.GPS_DEFAULT_MIN_DISTANCE_DELAY_METERS : runningProtocols.Min(p => p.GpsMinDistanceDelayMeters);
			}
		}

		#region platform-specific properties

		[JsonIgnore]
		public abstract bool IsCharging { get; }

		[JsonIgnore]
		public abstract float BatteryChargePercent { get; }

		[JsonIgnore]
		public abstract bool WiFiConnected { get; }

		[JsonIgnore]
		public abstract string DeviceId { get; }

		[JsonIgnore]
		public abstract string OperatingSystem { get; }

		[JsonIgnore]
		public abstract string Version { get; }

		[JsonIgnore]
		public abstract string DeviceManufacturer { get; }

		[JsonIgnore]
		public abstract string DeviceModel { get; }

		#region iOS GPS listener settings

#if __IOS__
		[JsonIgnore]
		public bool GpsPauseLocationUpdatesAutomatically
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? false : runningProtocols.All(p => p.GpsPauseLocationUpdatesAutomatically);
			}
		}

		[JsonIgnore]
		public ActivityType GpsActivityType
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 || runningProtocols.Select(p => p.GpsPauseActivityType).Distinct().Count() > 1 ? ActivityType.Other : runningProtocols.First().GpsPauseActivityType;
			}
		}

		[JsonIgnore]
		public bool GpsListenForSignificantChanges
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? false : runningProtocols.All(p => p.GpsListenForSignificantChanges);
			}
		}

		[JsonIgnore]
		public bool GpsDeferLocationUpdates
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? false : runningProtocols.All(p => p.GpsDeferLocationUpdates);
			}
		}

		[JsonIgnore]
		public float GpsDeferralDistanceMeters
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? -1 : runningProtocols.Min(p => p.GpsDeferralDistanceMeters);
			}
		}

		[JsonIgnore]
		public float GpsDeferralTimeMinutes
		{
			get
			{
				List<Protocol> runningProtocols = GetRunningProtocols();
				return runningProtocols.Count == 0 ? -1 : runningProtocols.Min(p => p.GpsDeferralTimeMinutes);
			}
		}
#endif

		#endregion

		#endregion

		#region Constructors
		[JsonConstructor]
		protected SensusServiceHelper()
		{
			if (SINGLETON != null)
			{
				throw SensusException.Report("Attempted to construct new service helper when singleton already existed.");
			}

			_registeredProtocols = new ConcurrentObservableCollection<Protocol>();
			_scriptsToRun = new ConcurrentObservableCollection<Script>();
			_notifications = new ConcurrentObservableCollection<UserMessage>();
			_protocolStates = new Dictionary<string, ProtocolState>();
			_hasher = new SHA256Managed();
			_pointsOfInterest = new List<PointOfInterest>();
			_barcodeWriter = new BarcodeWriter
			{
				Format = BarcodeFormat.QR_CODE,
				Options = new ZXing.Common.EncodingOptions
				{
					Height = 500,
					Width = 500
				}
			};

			_flashNotificationsEnabled = true;

			if (!Directory.Exists(SHARE_DIRECTORY))
			{
				Directory.CreateDirectory(SHARE_DIRECTORY);
			}

#if DEBUG || UI_TESTING
			LoggingLevel loggingLevel = LoggingLevel.Debug;
#elif RELEASE
			LoggingLevel loggingLevel = LoggingLevel.Normal;
#else
#error "Unrecognized configuration."
#endif

			_logger = new Logger(LOG_PATH, loggingLevel, Console.Error);
			_logger.Log("Log file started at \"" + LOG_PATH + "\".", LoggingLevel.Normal, GetType());

			ServicePointManager.ServerCertificateValidationCallback += ServerCertificateValidationCallback;
		}
		#endregion

		private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if (certificate == null)
			{
				return false;
			}

			// check host/certificate against any protocols that are doing s3 certificate pinning. it is important to do this before the
			// whitelist check below, as a man-in-the-middle attacker would presumably be able to spoof the whitelisted host whereas we
			// wish to force the server to supply the expected public key instead.
			if (_registeredProtocols.Any(protocol =>
				{
					if (protocol.RemoteDataStore is AmazonS3RemoteDataStore)
					{
						AmazonS3RemoteDataStore amazonS3RemoteDataStore = protocol.RemoteDataStore as AmazonS3RemoteDataStore;

						if (!string.IsNullOrWhiteSpace(amazonS3RemoteDataStore.PinnedServiceURL) &&                 // if we're pinning
							certificate.Subject == "CN=" + new Uri(amazonS3RemoteDataStore.PinnedServiceURL).Host)  // if the certificate is from the host we are pinning
						{
							// check whether the certificate's public key is a mismatch for the one we are expecting
							return Convert.ToBase64String(certificate.GetPublicKey()) != amazonS3RemoteDataStore.PinnedPublicKey;
						}
					}

					return false;
				}))
			{
				return false;
			}

			// accept the certificate if there were no policy errors
			return sslPolicyErrors == SslPolicyErrors.None;
		}

		public string GetHash(string s)
		{
			if (s == null)
			{
				return null;
			}

			StringBuilder hashBuilder = new StringBuilder();
			foreach (byte b in _hasher.ComputeHash(Encoding.UTF8.GetBytes(s)))
			{
				hashBuilder.Append(b.ToString("x"));
			}

			return hashBuilder.ToString();
		}

		#region platform-specific methods. this functionality cannot be implemented in a cross-platform way. it must be done separately for each platform. we are gradually migrating this functionality into the ISensusContext object.

		public abstract Task KeepDeviceAwakeAsync();

		public abstract Task LetDeviceSleepAsync();

		protected abstract Task ProtectedFlashNotificationAsync(string message);

		public abstract Task ShareFileAsync(string path, string subject, string mimeType);

		public abstract Task SendEmailAsync(string toAddress, string subject, string message);

		public abstract Task TextToSpeechAsync(string text);

		public abstract Task<string> RunVoicePromptAsync(string prompt, Action postDisplayCallback);

		/// <summary>
		/// The user can enable all probes at once. When this is done, it doesn't make sense to enable, e.g., the
		/// listening location probe as well as the polling location probe. This method allows the platforms to
		/// decide which probes to enable when enabling all probes.
		/// </summary>
		/// <returns><c>true</c>, if probe should be enabled, <c>false</c> otherwise.</returns>
		/// <param name="probe">Probe.</param>
		public abstract bool EnableProbeWhenEnablingAll(Probe probe);

		public abstract ImageSource GetQrCodeImageSource(string contents);

		protected abstract Task RegisterWithNotificationHubAsync(Tuple<string, string> hubSas);

		protected abstract Task UnregisterFromNotificationHubAsync(Tuple<string, string> hubSas);

		protected abstract void RequestNewPushNotificationToken();

		public abstract Task<bool> EnableBluetoothAsync(bool lowEnergy, string rationale);

		public abstract Task<bool> DisableBluetoothAsync(bool reenable, bool lowEnergy, string rationale);
		#endregion

		#region add/remove running protocol ids

		public async Task AddRunningProtocolIdAsync(string id)
		{
			bool save = false;

			lock (_protocolStates)
			{
				if (_protocolStates.TryGetValue(id, out ProtocolState state) == false || state != ProtocolState.Running)
				{
					_protocolStates[id] = ProtocolState.Running;

					save = true;
				}
			}

			if (save)
			{
				await SaveAsync();
			}
		}

		public async Task RemoveRunningProtocolIdAsync(string id)
		{
			bool save = false;

			lock (_protocolStates)
			{
				if (_protocolStates.TryGetValue(id, out ProtocolState state) == false || state == ProtocolState.Running)
				{
					_protocolStates[id] = ProtocolState.Stopped;

					save = true;
				}
			}

			if (save)
			{
				await SaveAsync();
			}
		}

		public async Task AddPausedProtocolIdAsync(string id)
		{
			bool save = false;

			lock (_protocolStates)
			{
				if (_protocolStates.TryGetValue(id, out ProtocolState state) == false || state != ProtocolState.Paused)
				{
					_protocolStates[id] = ProtocolState.Paused;

					save = true;
				}
			}

			if (save)
			{
				await SaveAsync();
			}
		}

		public List<Protocol> GetRunningProtocols()
		{
			return _registeredProtocols.Where(p => p.State == ProtocolState.Running).ToList();
		}

		#endregion

		public Task SaveAsync()
		{
			return Task.Run(() =>
			{
				lock (_saveLocker)
				{
					if (_saving)
					{
						_logger.Log("Already saving. Aborting save.", LoggingLevel.Normal, GetType());
						return;
					}
					else
					{
						_saving = true;
					}
				}

				try
				{
					_logger.Log("Serializing service helper.", LoggingLevel.Normal, GetType());

					string serviceHelperJSON = JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS);

					// once upon a time, we made the poor decision to encode protocols as unicode (UTF-16). can't switch to UTF-8 now...
					byte[] encryptedBytes = SensusContext.Current.SymmetricEncryption.Encrypt(serviceHelperJSON, Encoding.Unicode);
					File.WriteAllBytes(SERIALIZATION_PATH, encryptedBytes);

					_logger.Log("Serialized service helper with " + _registeredProtocols.Count + " protocols.", LoggingLevel.Normal, GetType());

					// ensure that all logged messages make it into the file.
					_logger.CommitMessageBuffer();
				}
				catch (Exception ex)
				{
					string message = "Exception while serializing service helper:  " + ex;
					SensusException.Report(message, ex);
					_logger.Log(message, LoggingLevel.Normal, GetType());
				}
				finally
				{
					lock (_saveLocker)
					{
						_saving = false;
					}
				}
			});
		}

		/// <summary>
		/// Starts platform-independent service functionality, including protocols that should be running. Okay to call multiple times, even if the service is already running.
		/// </summary>
		public async Task StartAsync()
		{
			// initialize the health test callback if it hasn't already been done
			bool scheduleHealthTestCallback = false;
			lock (_healthTestCallbackLocker)
			{
				if (_healthTestCallback == null)
				{
					_healthTestCallback = new ScheduledCallback(async cancellationToken =>
					{
						// test running protocols. we used to test all protocols, but this causes problems when editing stopped
						// protocols, as they might be replaced without the user intending after the user manually sets the id.
						foreach (Protocol protocolToTest in GetRunningProtocols())
						{
							if (cancellationToken.IsCancellationRequested)
							{
								break;
							}

							_logger.Log("Sensus health test for protocol \"" + protocolToTest.Name + "\" (" + protocolToTest.Id + ") is running.", LoggingLevel.Normal, GetType());

							bool testCurrentProtocol = true;

							// if we're using an authentication service, check if the desired protocol has changed as indicated by 
							// the protocol id returned with credentials.
							if (protocolToTest.AuthenticationService != null)
							{
								try
								{
									// get fresh credentials and check the protocol ID
									AmazonS3Credentials testCredentials = await protocolToTest.AuthenticationService.GetCredentialsAsync();

									// we're getting app center errors indicating a null reference somewhere in this try clause. do some extra reporting.
									if (testCredentials == null)
									{
										throw new NullReferenceException("Returned test credentials are null.");
									}

									if (protocolToTest.Id != testCredentials.ProtocolId)
									{
										Logger.Log("Protocol identifier no longer matches that of credentials. Updating to new protocol.", LoggingLevel.Normal, GetType());

										// if the current protocol is starting or running, then we'll start the new protocol.
										bool startNewProtocol = protocolToTest.State == ProtocolState.Starting || protocolToTest.State == ProtocolState.Running;

										// delete the current protocol (this first stops it). previously, we waited to stop/delete the current
										// protocol until after we had started the new one; however, this is both confusing (the user was seeing
										// two protocols listed) as well as error prone (the protocols may share identifiers like those on 
										// scripts, which eventually make their way into callback IDs that might clash with those currently scheduled
										// as a result -- see issue #736 on github).
										await protocolToTest.DeleteAsync();

										// we just stopped/deleted the current protocol. don't bother testing it.
										testCurrentProtocol = false;

										// download the desired protocol. as we're initiating the download without user interaction, do not explicitly
										// offer to replace the existing protocol. if the identifier in the protocol that we download duplicates one on 
										// the current device, don't bother the user (as they did not initiate the action). an exception to be thrown.
										Protocol newProtocol = await Protocol.DeserializeAsync(new Uri(testCredentials.ProtocolURL), false, testCredentials);

										// wire up new protocol with the current authentication service
										newProtocol.AuthenticationService = protocolToTest.AuthenticationService;
										newProtocol.ParticipantId = protocolToTest.AuthenticationService.Account.ParticipantId;

										if (startNewProtocol)
										{
											await newProtocol.StartAsync(cancellationToken);
										}
									}
								}
								catch (Exception ex)
								{
									Logger.Log("Exception while checking for protocol change:  " + ex.Message, LoggingLevel.Normal, GetType());
								}
							}

							if (testCurrentProtocol)
							{
								await protocolToTest.TestHealthAsync(false, cancellationToken);
							}
						}

						// test the callback scheduler
						SensusContext.Current.CallbackScheduler.TestHealth();

						// update push notification registrations
						if (_updatePushNotificationRegistrationsOnNextHealthTest)
						{
							await UpdatePushNotificationRegistrationsAsync(cancellationToken);
						}

						// test the notifier, which checks the push notification requests.
						await SensusContext.Current.Notifier.TestHealthAsync(cancellationToken);

					}, HEALTH_TEST_DELAY, HEALTH_TEST_DELAY, "HEALTH-TEST", GetType().FullName, null, TimeSpan.FromMinutes(5), TimeSpan.Zero, TimeSpan.Zero, ScheduledCallbackPriority.Low, GetType());  // we use the health test count to measure participation. don't tolerate any delay in the callback.

					scheduleHealthTestCallback = true;
				}
			}

			if (scheduleHealthTestCallback)
			{
				await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(_healthTestCallback);
			}

			foreach (Protocol registeredProtocol in _registeredProtocols)
			{
				/*if (registeredProtocol.State == ProtocolState.Stopped && _protocolStates.Contains(registeredProtocol.Id))
				{
					// don't present the user with an interface. just start up in the background.
					await registeredProtocol.StartAsync(CancellationToken.None);
				}*/

				if (_protocolStates.TryGetValue(registeredProtocol.Id, out ProtocolState state))
				{
					if (state == ProtocolState.Running)
					{
						await registeredProtocol.StartAsync(CancellationToken.None);
					}
					else if (state == ProtocolState.Paused)
					{
						registeredProtocol.RestorePausedState();
					}
				}
			}
		}

		public void RegisterProtocol(Protocol protocol)
		{
			if (!_registeredProtocols.Contains(protocol))
			{
				_registeredProtocols.Add(protocol);
			}
		}

		public async Task AddScriptAsync(Script script, RunMode runMode)
		{
			//// shuffle input groups if needed, but only if there are no display conditions involved. display
			//// conditions assume that the input groups will be displayed in a particular order (i.e., non-shuffled).
			//// if a display condition is present and the groups are shuffled, then it could happen that a group's
			//// inputs (being conditioned on a subsequent group) are not shown.
			//Random random = new Random();
			//if (script.Runner.ShuffleInputGroups &&
			//	!script.InputGroups.SelectMany(inputGroup => inputGroup.Inputs)
			//					   .SelectMany(input => input.DisplayConditions)
			//					   .Any())
			//{
			//	random.Shuffle(script.InputGroups);
			//}

			//// shuffle inputs in groups if needed. it's fine to shuffle the inputs within a group even when there
			//// are display conditions, as display conditions only hold between inputs in different groups.
			//foreach (InputGroup inputGroup in script.InputGroups)
			//{
			//	if (inputGroup.ShuffleInputs)
			//	{
			//		random.Shuffle(inputGroup.Inputs);
			//	}
			//}

			script.Shuffle();

			bool modifiedScriptsToRun = false;

			// scripts can be added from several threads, particularly on ios when several script runs can execute concurrently when
			// the user opens the app. lock modifications of the collection for safety.
			_scriptsToRun.Concurrent.ExecuteThreadSafe(() =>
			{
				if (runMode == RunMode.Multiple)
				{
					_scriptsToRun.Insert(GetScriptIndex(script), script);
					modifiedScriptsToRun = true;
				}
				else
				{
					List<Script> scriptsFromSameRunner = _scriptsToRun.Where(scriptToRun => scriptToRun.Runner.Script.Id == script.Runner.Script.Id).ToList();
					scriptsFromSameRunner.Add(script);

					Script scriptToKeep = null;
					List<Script> scriptsToRemove = null;

					if (runMode == RunMode.SingleKeepOldest)
					{
						scriptToKeep = scriptsFromSameRunner.First();
						scriptsToRemove = scriptsFromSameRunner.Skip(1).ToList();
					}
					else if (runMode == RunMode.SingleKeepNewest)
					{
						scriptToKeep = scriptsFromSameRunner.Last();
						scriptsToRemove = scriptsFromSameRunner.Take(scriptsFromSameRunner.Count - 1).ToList();
					}
					else
					{
						SensusException.Report("Unrecognized RunMode:  " + runMode);
						return;
					}

					foreach (Script scriptToRemove in scriptsToRemove)
					{
						if (_scriptsToRun.Remove(scriptToRemove))
						{
							modifiedScriptsToRun = true;
						}
					}

					if (!_scriptsToRun.Contains(scriptToKeep))
					{
						_scriptsToRun.Insert(GetScriptIndex(scriptToKeep), scriptToKeep);
						modifiedScriptsToRun = true;
					}
				}
			});

			if (modifiedScriptsToRun)
			{
				// clear out any expired scripts
				await RemoveExpiredScriptsAsync();

				// update the pending surveys notification
				await IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.BadgeTextAlert, script.Runner.Probe.Protocol);

				// save the app state. if the app crashes we want to keep the surveys around so they can be 
				// taken. this will not result in duplicate surveys in cases where the script probe restarts
				// and reschedules itself, as the the script probe schedule only concerns future surveys 
				// whereas the surveys serialized within the app state within _scriptsToRun are by definition
				// surveys deployed in the past.
				try
				{
					await SaveAsync();
				}
				catch (Exception ex)
				{
					SensusException.Report("Exception while saving app state after adding survey:  " + ex.Message, ex);
				}
			}
		}

		public bool RemoveScriptsForRunner(ScriptRunner runner)
		{
			return RemoveScripts(_scriptsToRun.Where(script => script.Runner == runner).ToArray());
		}

		public async Task<bool> RemoveExpiredScriptsAsync()
		{
			bool removed = false;

			foreach (Script script in _scriptsToRun)
			{
				if (script.Expired)
				{
					// let the script agent know and store a datum to record the event
					await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Expired) ?? Task.CompletedTask);
					script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Expired, DateTimeOffset.UtcNow, script), CancellationToken.None);

					if (RemoveScripts(script))
					{
						removed = true;
					}
				}
			}

			return removed;
		}

		public async Task<bool> ClearScriptsAsync()
		{
			foreach (Script script in _scriptsToRun)
			{
				// let the script agent know and store a datum to record the event
				await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Deleted) ?? Task.CompletedTask);
				script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Deleted, DateTimeOffset.UtcNow, script), CancellationToken.None);
			}

			return RemoveScripts(_scriptsToRun.ToArray());
		}

		public async Task IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode notificationMode, Protocol protocol)
		{
			// clear any existing notifications/badges
			CancelPendingSurveysNotification();

			await _scriptsToRun.Concurrent.ExecuteThreadSafe(async () =>
			{
				int numScriptsToRun = _scriptsToRun.Count;

				if (numScriptsToRun > 0 && notificationMode != PendingSurveyNotificationMode.None)
				{
					string s = numScriptsToRun == 1 ? "" : "s";
					string pendingSurveysTitle = numScriptsToRun == 0 ? null : $"You have {numScriptsToRun} pending survey{s}.";
					DateTime? nextExpirationDate = _scriptsToRun.Select(script => script.ExpirationDate).Where(expirationDate => expirationDate.HasValue).OrderBy(expirationDate => expirationDate).FirstOrDefault();
					string nextExpirationMessage = nextExpirationDate == null ? (numScriptsToRun == 1 ? "This survey does" : "These surveys do") + " not expire." : "Next expiration:  " + nextExpirationDate.Value.ToShortDateString() + " at " + nextExpirationDate.Value.ToShortTimeString();

					string notificationId = null;
					bool alertUser = false;

					if (notificationMode == PendingSurveyNotificationMode.Badge)
					{
						notificationId = Notifier.PENDING_SURVEY_BADGE_NOTIFICATION_ID;
					}
					else if (notificationMode == PendingSurveyNotificationMode.BadgeText)
					{
						notificationId = Notifier.PENDING_SURVEY_TEXT_NOTIFICATION_ID;
						alertUser = false;
					}
					else if (notificationMode == PendingSurveyNotificationMode.BadgeTextAlert)
					{
						notificationId = Notifier.PENDING_SURVEY_TEXT_NOTIFICATION_ID;
						alertUser = true;
					}
					else
					{
						SensusException.Report("Unrecognized pending survey notification mode:  " + notificationMode);
						return;
					}

					await SensusContext.Current.Notifier.IssueNotificationAsync(pendingSurveysTitle, nextExpirationMessage, notificationId, alertUser, protocol, numScriptsToRun, NotificationUserResponseAction.DisplayPendingSurveys, null);
				}
			});
		}

		public void CancelPendingSurveysNotification()
		{
			SensusContext.Current.Notifier.CancelNotification(Notifier.PENDING_SURVEY_TEXT_NOTIFICATION_ID);
			SensusContext.Current.Notifier.CancelNotification(Notifier.PENDING_SURVEY_BADGE_NOTIFICATION_ID);

#if __IOS__
			// clear the budge -- must be done from UI thread
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
			{
				UIKit.UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
			});
#endif
		}

		/// <summary>
		/// Flashes a notification.
		/// </summary>
		/// <returns>The notification async.</returns>
		/// <param name="message">Message.</param>
		public async Task FlashNotificationAsync(string message)
		{
			// do not show flash notifications when UI testing, as they can disrupt UI scripting on iOS.
#if !UI_TESTING
			if (_flashNotificationsEnabled)
			{
				await ProtectedFlashNotificationAsync(message);
			}
#endif
		}

		public async Task<string> ScanQrCodeAsync(string resultPrefix)
		{
			TaskCompletionSource<string> resultCompletionSource = new TaskCompletionSource<string>();

			await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
			{
				// we've seen exceptions where we don't ask for permission, leaving this up to the ZXing library
				// to take care of. the library does ask for permission, but if it's denied we get an exception
				// kicked back. ask explicitly here, and bail out if permission is not granted.
				if (await ObtainPermissionAsync(Permission.Camera) != PermissionStatus.Granted)
				{
					resultCompletionSource.TrySetResult(null);
					return;
				}

				// TODO:  there's a race condition bug in the scanning library:  https://github.com/Redth/ZXing.Net.Mobile/issues/717
				// delaying a bit seems to fix it.
				await Task.Delay(1000);

				Button cancelButton = new Button
				{
					Text = "Cancel",
					FontSize = 30
				};

				StackLayout scannerOverlay = new StackLayout
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					Padding = new Thickness(30),
					Children = { cancelButton }
				};

				ZXingScannerPage barcodeScannerPage = new ZXingScannerPage(new MobileBarcodeScanningOptions
				{
					PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE }.ToList()

				}, scannerOverlay);

				INavigation navigation = (Application.Current as App).DetailPage.Navigation;

				Func<Task> closeScannerPageAsync = new Func<Task>(async () =>
				{
					await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
					{
						barcodeScannerPage.IsScanning = false;

						// we've seen a strange race condition where the QR code input scanner button is 
						// pressed, and in the above task delay the input group page is cancelled and 
						// another UI button is hit before the scanner page comes up.
						if (navigation.ModalStack.LastOrDefault() == barcodeScannerPage)
						{
							await navigation.PopModalAsync();
						}
					});
				});

				cancelButton.Clicked += async (o, e) =>
				{
					await closeScannerPageAsync();
				};

				string result = null;

				barcodeScannerPage.OnScanResult += async r =>
				{
					if (resultPrefix == null || r.Text.StartsWith(resultPrefix))
					{
						result = r.Text.Substring(resultPrefix?.Length ?? 0).Trim();

						await closeScannerPageAsync();
					}
				};

				barcodeScannerPage.Disappearing += (sender, e) =>
				{
					resultCompletionSource.TrySetResult(result);
				};

				await navigation.PushModalAsync(barcodeScannerPage);
			});

			return await resultCompletionSource.Task;
		}

		public async Task<string> PromptForAndReadTextFileAsync()
		{
			string text = null;

			try
			{
				FileData data = await CrossFilePicker.Current.PickFile();

				if (data != null)
				{
					text = Encoding.UTF8.GetString(data.DataArray);
				}
			}
			catch (Exception ex)
			{
				string message = "Error choosing file: " + ex.Message;
				Logger.Log(message, LoggingLevel.Normal, GetType());
				await FlashNotificationAsync(message);
			}

			return text;
		}

		public async Task<Input> PromptForInputAsync(string windowTitle, Input input, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress)
		{
			List<Input> inputs = await PromptForInputsAsync(windowTitle, new[] { input }, cancellationToken, showCancelButton, nextButtonText, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress);
			return inputs?.First();
		}

		public async Task<List<Input>> PromptForInputsAsync(string windowTitle, IEnumerable<Input> inputs, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress)
		{
			InputGroup inputGroup = new InputGroup { Name = windowTitle };

			foreach (var input in inputs)
			{
				inputGroup.Inputs.Add(input);
			}

			IEnumerable<InputGroup> inputGroups = await PromptForInputsAsync(null, null, new[] { inputGroup }, cancellationToken, showCancelButton, nextButtonText, true, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, false, null);

			return inputGroups?.SelectMany(g => g.Inputs).ToList();
		}

		public async Task<IEnumerable<InputGroup>> PromptForInputsAsync(DateTimeOffset? firstPromptTimestamp, string title, IEnumerable<InputGroup> inputGroups, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, bool confirmNavigation, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, bool useDetailPage, Action postDisplayCallback)
		{
			PromptForInputsResult result = await PromptForInputsAsync(firstPromptTimestamp, title, inputGroups, cancellationToken, showCancelButton, nextButtonText, confirmNavigation, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, useDetailPage, postDisplayCallback, null);

			return result.InputGroups;
		}

		protected class PromptForInputsResult
		{
			public IEnumerable<InputGroup> InputGroups { get; set; }
			public InputGroupPage.NavigationResult NavigationResult { get; set; }
		}

		private Page GetReturnPage(Page detailPage)
		{
			if (detailPage is InputGroupPage previousInputGroupPage)
			{
				return previousInputGroupPage.ReturnPage;
			}
			else if (detailPage is NavigationPage previousNavigationPage && previousNavigationPage.CurrentPage is InputGroupPage previousCurrentPage)
			{
				return previousCurrentPage.ReturnPage;
			}

			return detailPage;
		}

		protected async Task<PromptForInputsResult> PromptForInputsAsync(DateTimeOffset? firstPromptTimestamp, string title, IEnumerable<InputGroup> inputGroups, CancellationToken? cancellationToken, bool showCancelButton, string nextButtonText, bool confirmNavigation, string cancelConfirmation, string incompleteSubmissionConfirmation, string submitConfirmation, bool displayProgress, bool useDetailPage, Action postDisplayCallback, SavedScriptState savedState)
		{
			bool firstPageDisplay = true;
			App app = Application.Current as App;
			INavigation navigation = app.DetailPage.Navigation;
			Page currentPage = null;
			Page returnPage = GetReturnPage(app.DetailPage);
			InputGroupPage.NavigationResult lastNavigationResult = InputGroupPage.NavigationResult.None;
			int startInputGroupIndex = 0;

			// keep a stack of input groups that were displayed so that the user can navigate backward. not all groups are displayed due to display
			// conditions, so we can't simply decrement the index to navigate backwards.
			Stack<int> inputGroupBackStack = new Stack<int>();

			// assign inputs to scoreinputs by scoregroup
			IEnumerable<Input> allInputs = inputGroups.SelectMany(x => x.Inputs);
			IEnumerable<ScoreInput> allScoreInputs = allInputs.OfType<ScoreInput>();
			ILookup<string, ScoreInput> scoreInputLookup = allScoreInputs.ToLookup(x =>
			{
				if (string.IsNullOrWhiteSpace(x.ScoreGroup))
				{
					return null;
				}

				return x.ScoreGroup;
			});

			foreach (ScoreInput scoreInput in allInputs.OfType<ScoreInput>())
			{
				scoreInput.ClearInputs();
			}

			IEnumerable<ScoreInput> groupedScoreInputs = allInputs.OfType<ScoreInput>().Where(x => string.IsNullOrWhiteSpace(x.ScoreGroup) == false);

			// if the score group key is null, then the ScoreKeeperInputs accumulates the score of the other ScoreInputs in the collection of InputGroups
			foreach (ScoreInput scoreInput in scoreInputLookup[null])
			{
				scoreInput.Inputs = groupedScoreInputs;
			}

			if (savedState != null)
			{
				// put the saved input group positions onto the local stack and have the state managed by the presentation loop.
				inputGroupBackStack = savedState.InputGroupStack;

				if (savedState.InputGroupStack.Any())
				{
					startInputGroupIndex = savedState.InputGroupStack.FirstOrDefault() + 1;
				}
			}

			bool continueRun = true;

			for (int inputGroupIndex = startInputGroupIndex; continueRun && inputGroupIndex < inputGroups.Count() && !cancellationToken.GetValueOrDefault().IsCancellationRequested; ++inputGroupIndex)
			{
				InputGroup inputGroup = inputGroups.ElementAt(inputGroupIndex);

				// run voice inputs by themselves, and only if the input group contains exactly one input and that input is a voice input.
				if (inputGroup.Inputs.Count == 1 && inputGroup.Inputs[0] is VoiceInput)
				{
					VoiceInput voiceInput = inputGroup.Inputs[0] as VoiceInput;

					if (voiceInput.Enabled && voiceInput.Display)
					{
						try
						{
							// only run the post-display callback the first time a page is displayed. the caller expects the callback
							// to fire only once upon first display.
							await voiceInput.RunAsync(firstPromptTimestamp, firstPageDisplay ? postDisplayCallback : null);
							firstPageDisplay = false;
						}
						catch (Exception ex)
						{
							SensusException.Report("Voice input failed to run.", ex);
						}
					}
				}
				else
				{
					await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
					{
						int stepNumber = inputGroupIndex + 1;

						InputGroupPage inputGroupPage = new InputGroupPage(inputGroup, stepNumber, inputGroups.Count(), inputGroupBackStack.Count > 0, showCancelButton, nextButtonText, cancellationToken, confirmNavigation, cancelConfirmation, incompleteSubmissionConfirmation, submitConfirmation, displayProgress, title, savedState != null);

						// do not display prompts page under the following conditions:
						//
						// 1) there are no inputs displayed on it
						// 2) the cancellation token has requested a cancellation.
						//
						// if either of these conditions is true, continue to the next input group.

						if (inputGroupPage.DisplayedInputCount == 0)
						{
							// if we're on the final input group and no inputs were shown, then we're at the end and we're ready to submit the 
							// users' responses. first check that the user is ready to submit. if the user isn't ready then move back to the previous 
							// input group in the backstack, if there is one.
							if (inputGroupIndex >= inputGroups.Count() - 1 &&                                                     // this is the final input group
								inputGroupBackStack.Count > 0 &&                                                             // there is an input group to go back to (the current one was not displayed)
								!string.IsNullOrWhiteSpace(submitConfirmation) &&                                               // we have a submit confirmation
								confirmNavigation &&                                                                            // we should confirm submission
								!(await Application.Current.MainPage.DisplayAlert("Confirm", submitConfirmation, "Yes", "No"))) // user is not ready to submit
							{
								inputGroupIndex = inputGroupBackStack.Pop() - 1;
							}
						}
						// display the page if we've not been canceled
						else if (!cancellationToken.GetValueOrDefault().IsCancellationRequested)
						{
							foreach (Input input in inputGroup.Inputs)
							{
								if (input is not ScoreInput && string.IsNullOrWhiteSpace(input.ScoreGroup) == false)
								{
									foreach (ScoreInput scoreInput in scoreInputLookup[input.ScoreGroup])
									{
										if (scoreInput.Inputs.Contains(input) == false)
										{
											scoreInput.AddInput(input);
										}
									}
								}
							}

							currentPage = inputGroupPage;

							// display page. only animate the display for the first page.
							if (inputGroup.UseNavigationBar)
							{
								currentPage = new NavigationPage(currentPage);
							}

							// prepare the page
							await inputGroupPage.PrepareAsync();

							if (useDetailPage)
							{
								inputGroupPage.ReturnPage = returnPage;

								app.DetailPage = currentPage;
							}
							else
							{
								await navigation.PushModalAsync(currentPage, firstPageDisplay);
							}

							// save the state to file
							if (savedState != null)
							{
								await savedState.SaveAsync();
							}

							// only run the post-display callback the first time a page is displayed. the caller expects the callback
							// to fire only once upon first display.
							if (firstPageDisplay)
							{
								postDisplayCallback?.Invoke();
								firstPageDisplay = false;
							}

							lastNavigationResult = await inputGroupPage.ResponseTask;

							await inputGroupPage.DisposeAsync();

							if (savedState == null && lastNavigationResult == InputGroupPage.NavigationResult.Paused)
							{
								lastNavigationResult = InputGroupPage.NavigationResult.Cancel;
							}

							_logger.Log("Input group page navigation result:  " + lastNavigationResult, LoggingLevel.Normal, GetType());

							if (lastNavigationResult == InputGroupPage.NavigationResult.Backward)
							{
								// we only allow backward navigation when we have something on the back stack. so the following is safe.
								inputGroupIndex = inputGroupBackStack.Pop() - 1;
							}
							else if (lastNavigationResult == InputGroupPage.NavigationResult.Forward || lastNavigationResult == InputGroupPage.NavigationResult.Timeout)
							{
								// keep the group in the back stack.
								inputGroupBackStack.Push(inputGroupIndex);
							}
							else if (lastNavigationResult == InputGroupPage.NavigationResult.Cancel)
							{
								inputGroups = null;
							}

							// there's nothing to do if the navigation result is submit, since we've finished the final
							// group and we are about to return.
						}
					});

					continueRun = lastNavigationResult != InputGroupPage.NavigationResult.Paused && inputGroups != null;
				}
			}

			if (useDetailPage)
			{
				if (app.DetailPage == currentPage && lastNavigationResult != InputGroupPage.NavigationResult.Paused)
				{
					app.DetailPage = returnPage;
				}
			}
			else
			{
				// animate pop if the user submitted or canceled. when doing this, reference the navigation context
				// on the page rather than the local 'navigation' variable. this is necessary because the navigation
				// context may have changed (e.g., if prior to the pop the user reopens the app via pending survey 
				// notification.

				foreach (Page modalPage in navigation.ModalStack.ToList())
				{
					await navigation.PopModalAsync(navigation.ModalStack.Count == 1);
				}
			}

			// process the inputs if the user didn't cancel
			if (lastNavigationResult == InputGroupPage.NavigationResult.Submit)
			{
				// set the submission timestamp. do this before GPS tagging since the latter could take a while and we want the timestamp to 
				// reflect the time that the user hit submit.
				DateTimeOffset submissionTimestamp = DateTimeOffset.UtcNow;
				foreach (InputGroup inputGroup in inputGroups)
				{
					foreach (Input input in inputGroup.Inputs)
					{
						input.SubmissionTimestamp = submissionTimestamp;
					}
				}

				#region geotag input groups if we've got input groups with inputs that are complete and lacking locations
				if (inputGroups.Any(inputGroup => inputGroup.Geotag && inputGroup.Inputs.Any(input => input.Complete && (input.Latitude == null || input.Longitude == null))))
				{
					_logger.Log("Geotagging input groups.", LoggingLevel.Normal, GetType());

					try
					{
						Position currentPosition = await GpsReceiver.Get().GetReadingAsync(cancellationToken.GetValueOrDefault(), true);

						if (currentPosition != null)
						{
							foreach (InputGroup inputGroup in inputGroups)
							{
								if (inputGroup.Geotag)
								{
									foreach (Input input in inputGroup.Inputs)
									{
										if (input.Complete)
										{
											bool locationUpdated = false;

											if (input.Latitude == null)
											{
												input.Latitude = currentPosition.Latitude;
												locationUpdated = true;
											}

											if (input.Longitude == null)
											{
												input.Longitude = currentPosition.Longitude;
												locationUpdated = true;
											}

											if (locationUpdated)
											{
												input.LocationUpdateTimestamp = currentPosition.Timestamp;
											}
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						_logger.Log("Error geotagging input groups:  " + ex.Message, LoggingLevel.Normal, GetType());
					}
				}
				#endregion
			}

			return new PromptForInputsResult { InputGroups = inputGroups, NavigationResult = lastNavigationResult };
		}

		public void GetPositionsFromMapAsync(Xamarin.Forms.Maps.Position address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
		{
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
			{
				if (await ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
				{
					await FlashNotificationAsync("Geolocation is not permitted on this device. Cannot display map.");
				}
				else
				{
					MapPage mapPage = new MapPage(address, newPinName);

					mapPage.Disappearing += (o, e) =>
					{
						callback(mapPage.Pins.Select(pin => pin.Position).ToList());
					};

					await (Application.Current as App).DetailPage.Navigation.PushModalAsync(mapPage);
				}
			});
		}

		public void GetPositionsFromMapAsync(string address, string newPinName, Action<List<Xamarin.Forms.Maps.Position>> callback)
		{
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
			{
				if (await ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
				{
					await FlashNotificationAsync("Geolocation is not permitted on this device. Cannot display map.");
				}
				else
				{
					MapPage mapPage = new MapPage(address, newPinName);

					mapPage.Disappearing += (o, e) =>
					{
						callback(mapPage.Pins.Select(pin => pin.Position).ToList());
					};

					await (Application.Current as App).DetailPage.Navigation.PushModalAsync(mapPage);
				}
			});
		}

		public Task UnregisterProtocolAsync(Protocol protocol)
		{
			_registeredProtocols.Remove(protocol);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets the share path with an extension.
		/// </summary>
		/// <returns>The share path.</returns>
		/// <param name="extension">Extension (with or without preceding ".")</param>
		public string GetSharePath(string extension)
		{
			lock (_shareFileLocker)
			{
				int fileNum = 0;
				string path = null;
				while (path == null || File.Exists(path))
				{
					path = Path.Combine(SHARE_DIRECTORY, fileNum++ + (string.IsNullOrWhiteSpace(extension) ? "" : "." + extension.Trim('.')));
				}

				return path;
			}
		}

		public string ConvertJsonForCrossPlatform(string json)
		{
			string currentTypeName = GetType().Name;

			StringBuilder convertedJSON = new StringBuilder(json.Length * 2);
			bool conversionPerformed = false;

			// run through each line in the JSON and modify .NET types appropriately. json.net escapes \r and \n when serializing, so we can safely split on these characters.
			foreach (string jsonLine in json.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (jsonLine.Trim().StartsWith("\"$type\":"))
				{
					// convert platform namespace
					string convertedJsonLine;
					if (currentTypeName == "AndroidSensusServiceHelper")
					{
						convertedJsonLine = jsonLine.Replace("iOS", "Android").Replace("WinPhone", "Android");
					}
					else if (currentTypeName == "iOSSensusServiceHelper")
					{
						convertedJsonLine = jsonLine.Replace("Android", "iOS").Replace("WinPhone", "iOS");
					}
					else
					{
						throw SensusException.Report("Attempted to convert JSON for unknown service helper type:  " + GetType().FullName);
					}

					if (convertedJsonLine != jsonLine)
					{
						conversionPerformed = true;
					}

					convertedJSON.AppendLine(convertedJsonLine);
				}
				else
				{
					convertedJSON.AppendLine(jsonLine);
				}
			}

			if (conversionPerformed)
			{
				_logger.Log("Performed cross-platform conversion of JSON.", LoggingLevel.Normal, GetType());
			}
			else
			{
				_logger.Log("No cross-platform conversion required for JSON.", LoggingLevel.Normal, GetType());
			}

			return convertedJSON.ToString();
		}

		public async Task<PermissionStatus> ObtainPermissionAsync(Permission permission)
		{
			return await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
			{
				if (await CrossPermissions.Current.CheckPermissionStatusAsync(permission) == PermissionStatus.Granted)
				{
					return PermissionStatus.Granted;
				}

				// if the user has previously denied our permission request, then we should be given an opportunity to
				// display a rationale for the request. if the user has selected the "don't ask again" option, then
				// we will not be able to display the rationale and all requests for the permission will fail.
				if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(permission))
				{
					string rationale = null;

					if (permission == Permission.Calendar)
					{
						rationale = "Sensus collects calendar information for studies you enroll in.";
					}
					else if (permission == Permission.Camera)
					{
						rationale = "Sensus uses the camera to scan barcodes. Sensus will not record images or video.";
					}
					else if (permission == Permission.Contacts)
					{
						rationale = "Sensus collects calendar information for studies you enroll in.";
					}
					else if (permission == Permission.Location)
					{
						rationale = "Sensus uses GPS to collect location information for studies you enroll in.";
					}
					else if (permission == Permission.LocationAlways)
					{
						rationale = "Sensus uses GPS to collect location information for studies you enroll in.";
					}
					else if (permission == Permission.LocationWhenInUse)
					{
						rationale = "Sensus uses GPS to collect location information for studies you enroll in.";
					}
					else if (permission == Permission.MediaLibrary)
					{
						rationale = "Sensus collects media for studies you enroll in.";
					}
					else if (permission == Permission.Microphone)
					{
						rationale = "Sensus uses the microphone to collect sound level information for studies you enroll in. Sensus will not record audio.";
					}
					else if (permission == Permission.Phone)
					{
						rationale = "Sensus collects call information for studies you enroll in. Sensus will not record audio from calls.";
					}
					else if (permission == Permission.Photos)
					{
						rationale = "Sensus collects photos for studies you enroll in.";
					}
					else if (permission == Permission.Reminders)
					{
						rationale = "Sensus collects reminder information for studies you enroll in.";
					}
					else if (permission == Permission.Sensors)
					{
						rationale = "Sensus uses movement sensors to collect information for studies you enroll in.";
					}
					else if (permission == Permission.Sms)
					{
						rationale = "Sensus collects text messages for studies you enroll in.";
					}
					else if (permission == Permission.Speech)
					{
						rationale = "Sensus uses the microphone for studies you enroll in.";
					}
					else if (permission == Permission.Storage)
					{
						rationale = "Sensus must be able to write to your device's storage for proper operation.";
					}
					else if (permission == Permission.Contacts)
					{
						rationale = "Sensus collects Contact information.";
					}
					else
					{
						SensusException.Report("Missing rationale for permission request:  " + permission);
					}

					if (rationale != null)
					{
						await Application.Current.MainPage.DisplayAlert("Permission Request", $"On the next screen, Sensus will request access to your device's {permission.ToString().ToUpper()}. {rationale}", "OK");
					}
				}

				try
				{
					PermissionStatus permissionStatus;

					// it's happened that the returned dictionary doesn't contain an entry for the requested permission, so check for that.
					if (!(await CrossPermissions.Current.RequestPermissionsAsync(permission)).TryGetValue(permission, out permissionStatus))
					{
						throw new Exception($"Permission status not returned for request:  {permission}");
					}

					return permissionStatus;
				}
				catch (Exception ex)
				{
					_logger.Log($"Failed to obtain permission:  {ex.Message}", LoggingLevel.Normal, GetType());

					return PermissionStatus.Unknown;
				}
			});
		}

		public async Task UpdatePushNotificationRegistrationsAsync(CancellationToken cancellationToken)
		{
			// the code we need exclusive access to below has an await statement in it, so we
			// can't lock the entire function. use a gatekeeper to gain exclusive access
			// and be sure to release the keeper below in the finally clause.
			lock (_updatePushNotificationRegistrationsLocker)
			{
				if (_updatingPushNotificationRegistrations)
				{
					return;
				}
				else
				{
					_updatingPushNotificationRegistrations = true;
				}
			}

			try
			{
				// assume everything is going to be fine and that we won't need to request 
				// an update on next health test. if the push notification token is not set
				// we'll throw an exception below and request a new token. when this new token 
				// arrives, we'll be right back here and we'll proceed with the registration update.
				_updatePushNotificationRegistrationsOnNextHealthTest = false;

				// we should always have a token. if we do not, throw an exception and request a new token.
				if (PushNotificationToken == null)
				{
					try
					{
						SensusException.Report("Push notification token was not set.");
					}
					catch (Exception)
					{
					}

					try
					{
						RequestNewPushNotificationToken();
					}
					catch (Exception newTokenException)
					{
						Logger.Log("Exception while requesting a new token:  " + newTokenException.Message, LoggingLevel.Normal, GetType());
					}
				}
				else
				{
					// it is conceivable that a single hub could be used for multiple protocols. because 
					// there is only ever a single registration with each hub, we therefore need to 
					// build a mapping from each hub to its protocols so we can determine whether we
					// actually need to register with the hub.
					Dictionary<Tuple<string, string>, List<Protocol>> hubSasProtocols = new Dictionary<Tuple<string, string>, List<Protocol>>();
					foreach (Tuple<string, string, Protocol> hubSasProtocol in _registeredProtocols.Select(protocol => new Tuple<string, string, Protocol>(protocol.PushNotificationsHub, protocol.PushNotificationsSharedAccessSignature, protocol)))
					{
						if (!string.IsNullOrWhiteSpace(hubSasProtocol.Item1) && !string.IsNullOrWhiteSpace(hubSasProtocol.Item2))
						{
							Tuple<string, string> hubSas = new Tuple<string, string>(hubSasProtocol.Item1, hubSasProtocol.Item2);

							if (!hubSasProtocols.ContainsKey(hubSas))
							{
								hubSasProtocols.Add(hubSas, new List<Protocol>());
							}

							hubSasProtocols[hubSas].Add(hubSasProtocol.Item3);
						}
					}

					// process each hub
					foreach (Tuple<string, string> hubSas in hubSasProtocols.Keys)
					{
						// unregister from the hub, catching any exceptions.
						try
						{
							await UnregisterFromNotificationHubAsync(hubSas);
						}
						catch (Exception unregisterEx)
						{
							// no need to request an update on the next health test, as it was just 
							// the unregister that failed. as long as the registration below works, 
							// we should be fine.
							Logger.Log("Exception while unregistering from hub:  " + unregisterEx.Message, LoggingLevel.Normal, GetType());
						}

						// each protocol may have its own remote data store being monitored for push notification
						// requests. tokens are per device, so update the token in each protocol's remote store.
						bool atLeastOneProtocolRunning = false;
						foreach (Protocol protocol in hubSasProtocols[hubSas])
						{
							// this only applies to protocols with a remote data store (some might simply be 
							// incompletely configured, and those can be skipped).
							if (protocol.RemoteDataStore == null)
							{
								continue;
							}

							// catch any exceptions, as we might just be lacking an internet connection.
							try
							{
								if (protocol.State == ProtocolState.Starting ||  // the current method is called when starting the protocol. send token immediately.
									protocol.State == ProtocolState.Running ||   // send token if running
									protocol.StartIsScheduled)                   // send token if scheduled to run, so that we receive the PN for startup.
								{
									atLeastOneProtocolRunning = true;

									await protocol.RemoteDataStore.SendPushNotificationTokenAsync(PushNotificationToken, cancellationToken);
								}
								else
								{
									await protocol.RemoteDataStore.DeletePushNotificationTokenAsync(cancellationToken);
								}
							}
							catch (Exception updateTokenException)
							{
								Logger.Log("Exception while updating push notification token:  " + updateTokenException.Message, LoggingLevel.Normal, GetType());

								// we absolutely must update the token at the remote data store
								_updatePushNotificationRegistrationsOnNextHealthTest = true;
							}
						}

						// register with the hub if any of its associated protocols are running
						if (atLeastOneProtocolRunning)
						{
							// catch any exceptions from registering
							try
							{
								await RegisterWithNotificationHubAsync(hubSas);
							}
							catch (Exception registerEx)
							{
								Logger.Log("Exception while registering with hub:  " + registerEx.Message, LoggingLevel.Normal, GetType());

								// we absolutely must register with the hub
								_updatePushNotificationRegistrationsOnNextHealthTest = true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					Logger.Log("Exception while updating push notification registrations:  " + ex.Message, LoggingLevel.Normal, GetType());
				}
				catch (Exception)
				{
				}

				// we have just reported the issue to the app center crash api, so hopefully we'll 
				// see the problem there. one thing we can do is try to update the push notification 
				// registrations again on the next health test...so...
				_updatePushNotificationRegistrationsOnNextHealthTest = true;
			}
			finally
			{
				// we're done...let the next update proceed.
				_updatingPushNotificationRegistrations = false;
			}
		}

		/// <summary>
		/// Called when the system or user wishes to stop the app entirely. Will stop all protocols and clean up.
		/// </summary>
		/// <returns>Task</returns>
		public virtual async Task StopAsync()
		{
			Logger.Log("Stopping protocols.", LoggingLevel.Normal, GetType());

			foreach (Protocol protocol in _registeredProtocols.ToArray())
			{
				try
				{
					await protocol.StopAsync();
				}
				catch (Exception ex)
				{
					_logger.Log($"Failed to stop protocol \"{protocol.Name}\": {ex.Message}", LoggingLevel.Normal, GetType());
				}
			}

			Logger.Log("Unscheduling health test callback.", LoggingLevel.Normal, GetType());
			await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(_healthTestCallback);
		}

		public virtual async Task<bool> RunScriptAsync(Script script, bool manualRun)
		{
			bool submitted = false;

			script.Submitting = true;

			// let the script agent know and store a datum to record the event
			await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Opened) ?? Task.CompletedTask);
			script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Opened, DateTimeOffset.UtcNow, script), CancellationToken.None);

			// determine what happens with the script state.
			SavedScriptState savedState = await ScriptRunner.ManageStateAsync(script);

			PromptForInputsResult result = await PromptForInputsAsync(script.RunTime, script.Runner.Name, script.InputGroups, null, script.Runner.AllowCancel, null, script.Runner.ConfirmNavigation, null, script.Runner.IncompleteSubmissionConfirmation, script.Runner.SubmitConfirmation, script.Runner.DisplayProgress, script.Runner.UseDetailPage, null, savedState);

			if (result.NavigationResult == InputGroupPage.NavigationResult.Paused)
			{
				Logger.Log("\"" + script.Runner.Name + "\" was paused.", LoggingLevel.Normal, typeof(Script));

				await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Cancelled) ?? Task.CompletedTask);
				// temporary ScriptState value until the Nuget package is updated
				script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Paused, DateTimeOffset.UtcNow, script), CancellationToken.None);

				if (savedState != null && result.InputGroups != null)
				{
					if (savedState.InputGroupStack.Count == 0)
					{
						// don't save the state if the user never got passed the first input group.
						//script.Runner.SavedState = null;
						ScriptRunner.ClearSavedState(script);

						savedState = null;
					}
					else
					{
						InputGroup[] inputGroups = result.InputGroups.ToArray();

						foreach (int index in savedState.InputGroupStack)
						{
							InputGroup inputGroup = inputGroups[index];

							foreach (Input input in inputGroup.Inputs)
							{
								string key = $"{inputGroup.Id}.{input.Id}";

								savedState.SavedInputs[key] = new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow),
																				script.Runner.Script.Id,
																				script.Runner.Name,
																				input.GroupId,
																				input.Id,
																				script.Id,
																				input.LabelText,
																				input.Name,
																				input.Value,
																				script.CurrentDatum?.Id,
																				input.Latitude,
																				input.Longitude,
																				input.LocationUpdateTimestamp,
																				script.RunTime.Value,
																				input.CompletionRecords,
																				DateTimeOffset.UtcNow, // save this now, but overwrite it when the script is actually submitted
																				manualRun);
							}
						}
					}
				}

				script.Submitting = false;
			}
			else if (result.NavigationResult == InputGroupPage.NavigationResult.Submit || result.NavigationResult == InputGroupPage.NavigationResult.Cancel)
			{
				// the script has either been canceled or submitted, so the state can be cleared.
				//script.Runner.SavedState = null;
				ScriptRunner.ClearSavedState(script);

				// track script state and completions. do this immediately so that all timestamps are as accurate as possible.
				if (result.NavigationResult == InputGroupPage.NavigationResult.Cancel)
				{
					// let the script agent know and store a datum to record the event
					await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Cancelled) ?? Task.CompletedTask);
					script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Cancelled, DateTimeOffset.UtcNow, script), CancellationToken.None);
				}
				else if (result.NavigationResult == InputGroupPage.NavigationResult.Submit)
				{
					// let the script agent know and store a datum to record the event
					await (script.Runner.Probe.Agent?.ObserveAsync(script, ScriptState.Submitted) ?? Task.CompletedTask);
					script.Runner.Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Submitted, DateTimeOffset.UtcNow, script), CancellationToken.None);

					// track times when script is completely valid and wasn't cancelled by the user
					if (script.Valid)
					{
						// add completion time and remove all completion times before the participation horizon
						lock (script.Runner.CompletionTimes)
						{
							script.Runner.CompletionTimes.Add(DateTime.Now);
							script.Runner.CompletionTimes.RemoveAll(completionTime => completionTime < script.Runner.Probe.Protocol.ParticipationHorizon);
						}

						if (script.Runner.KeepUntilCompleted)
						{
							if (RemoveScripts(script))
							{
								await IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.Badge, script.Runner.Probe.Protocol);
							}
						}
					}

					if (await script.Runner.ScheduleScriptFromInputAsync(script) == false)
					{
						await script.Runner.ScheduleNextScriptToRunAsync();
					}

					script.Runner.HasSubmitted = true;
				}

				// process/store all inputs in the script
				bool inputStored = false;
				foreach (InputGroup inputGroup in script.InputGroups)
				{
					foreach (Input input in inputGroup.Inputs)
					{
						if (result.NavigationResult == InputGroupPage.NavigationResult.Cancel)
						{
							input.Reset();
						}
						else if (input.Store)
						{
							if (input.Complete == false && savedState != null)
							{
								if (savedState.SavedInputs.TryGetValue($"{inputGroup.Id}.{input.Id}", out ScriptDatum savedInput))
								{
									savedInput.SubmissionTimestamp = input.SubmissionTimestamp ?? DateTimeOffset.UtcNow;
									savedInput.Latitude = input.Latitude;
									savedInput.Longitude = input.Longitude;
									savedInput.LocationTimestamp = input.LocationUpdateTimestamp;

									await script.Runner.Probe.StoreDatumAsync(savedInput, CancellationToken.None);
								}
							}
							else if (input.Display)
							{
								// the _script.Id allows us to link the data to the script that the user created. it never changes. on the other hand, the script
								// that is passed into this method is always a copy of the user-created script. the script.Id allows us to link the various data
								// collected from the user into a single logical response. each run of the script has its own script.Id so that responses can be
								// grouped across runs. this is the difference between scriptId and runId in the following line.
								await script.Runner.Probe.StoreDatumAsync(new ScriptDatum(input.CompletionTimestamp.GetValueOrDefault(DateTimeOffset.UtcNow),
																								  script.Runner.Script.Id,
																								  script.Runner.Name,
																								  input.GroupId,
																								  input.Id,
																								  script.Id,
																								  input.LabelText,
																								  input.Name,
																								  input.Value,
																								  script.CurrentDatum?.Id,
																								  input.Latitude,
																								  input.Longitude,
																								  input.LocationUpdateTimestamp,
																								  script.RunTime.Value,
																								  input.CompletionRecords,
																								  input.SubmissionTimestamp.Value,
																								  manualRun), CancellationToken.None);
							}

							inputStored = true;
						}
					}
				}

				// remove the submitted script. this should be done before the script is marked 
				// as not submitting in order to prevent the user from reopening it.
				if (result.NavigationResult != InputGroupPage.NavigationResult.Cancel && script.Runner.KeepUntilCompleted == false)
				{
					if (RemoveScripts(script))
					{
						await IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.Badge, script.Runner.Probe.Protocol);
					}

					submitted = true;
				}

				// update UI to indicate that the script is no longer being submitted. this should 
				// be done after the script is removed in order to prevent the user from retaking the script.
				script.Submitting = false;

				// run a local-to-remote transfer if desired, respecting wifi requirements. do this after everything above, as it may take
				// quite some time to transfer the data depending on its size.
				if (inputStored && script.Runner.ForceRemoteStorageOnSurveySubmission)
				{
					Logger.Log("Forcing a local-to-remote transfer.", LoggingLevel.Normal, typeof(Script));
					await script.Runner.Probe.Protocol.RemoteDataStore.WriteLocalDataStoreAsync(CancellationToken.None);
				}

				Logger.Log("\"" + script.Runner.Name + "\" has completed processing.", LoggingLevel.Normal, typeof(Script));
			}

			return submitted;
		}

		public bool RemoveScripts(params Script[] scripts)
		{
			bool removed = false;

			foreach (Script script in scripts)
			{
				if (_scriptsToRun.Remove(script))
				{
					removed = true;
				}
			}

			return removed;
		}

		private int GetScriptIndex(Script script)
		{
			List<Script> scripts = _scriptsToRun.ToList();

			int index;

			if (scripts.Count == 0)
			{
				index = 0;
			}
			else if (scripts[scripts.Count - 1].CompareTo(script) <= 0)
			{
				index = scripts.Count;
			}
			else
			{
				index = scripts.BinarySearch(script);

				if (index < 0)
				{
					index = ~index;
				}
			}

			return index;
		}

		public abstract string GetMimeType(string path);
	}
}
