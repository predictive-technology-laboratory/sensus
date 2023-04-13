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
using Sensus.UI.UiProperties;
using System.Threading;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using Sensus.Exceptions;
using System.Text;
using System.Collections.Generic;
using Sensus.Extensions;
using Sensus.Notifications;
using Sensus.Authentication;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Sensus.DataStores.Remote
{
	/// <summary>
	/// 
	/// The Amazon S3 Remote Data Store allows Sensus to upload data from the device to [Amazon's Simple Storage Service (S3)](https://aws.amazon.com/s3). The 
	/// S3 service is a simple, non-relational storage system that is relatively cheap, easy to use, and robust.
	/// 
	/// # Prerequisites
	/// 
	///   * Sign up for an account with Amazon Web Services, if you don't have one already. The [Free Tier](https://aws.amazon.com/free) is sufficient.
	///   * Install the [AWS Command Line Interface(CLI)](https://aws.amazon.com/cli).
	///   * Download and unzip our [AWS configuration scripts](https://github.com/predictive-technology-laboratory/sensus/raw/develop/Scripts/ConfigureAWS.zip).
	///   * Run the following command to configure an S3 bucket for use within a Sensus Amazon S3 Remote Data Store, where `NAME` is an informative name
	///     (alphanumerics and dashes only) and `REGION` is the region in which your bucket will reside (e.g., `us-east-1`):
	/// 
	///     ```
	///     ./configure-s3.sh NAME REGION
	///     ```
	/// 
	///   * The previous command will create a bucket as well as an IAM group and user with write-only access to the bucket. If successful, the command will 
	///     output something like the following:
	/// 
	///     ```
	///     Done. Details:
	///       Sensus S3 bucket:  test-bucket-eee8ef46-5d6a-4508-b745-e6635d195a85
	///       Sensus S3 IAM account:  XXXX:XXXX
	///     ```
	/// 
	///   * The bucket and IAM account produced on the final line should be kept confidential. Use these values as <see cref="Bucket"/> and 
	///     <see cref="IamAccountString"/>, respectively.
	/// 
	/// # Downloading Data from Amazon S3
	/// 
	/// Install the [AWS Command Line Interface](http://aws.amazon.com/cli). Assuming you have created and populated an S3 bucket named `BUCKET` and 
	/// a folder named `FOLDER`, you can download all of your Sensus data in a few different ways:
	/// 
	///   1. You can use the functions (e.g., `sensus.sync.from.aws.s3`) in the [SensusR](https://cran.r-project.org/web/packages/SensusR/index.html) package.
	///   1. You can execute the following command to download everything to a directory named `data` on your desktop:
	/// 
	///      ```
	///      aws s3 cp --recursive s3://BUCKET/FOLDER ~/data
	///      ```
	/// 
	///   1. You can use a third-party application like [Bucket Explorer](http://www.bucketexplorer.com) to browse and download data from Amazon S3.
	/// 
	/// # Deconfiguration
	/// 
	/// If you are finished collecting data and you would like to prevent any future data submission, you can deconfigure the IAM group and user
	/// with the following command, where `BUCKET` corresponds to the Sensus S3 bucket name created above:
	/// 
	///   ```
	///   ./deconfigure-s3.sh BUCKET
	///   ```
	/// 
	/// The preceding command will not delete your bucket or data.
	/// 
	/// </summary>
	public class AmazonS3RemoteDataStore : RemoteDataStore
	{
		/// <summary>
		/// The data directory.
		/// </summary>
		public const string DATA_DIRECTORY = "data";

		/// <summary>
		/// The push notifications directory.
		/// </summary>
		public const string PUSH_NOTIFICATIONS_DIRECTORY = "push-notifications";

		/// <summary>
		/// The push notifications tokens directory.
		/// </summary>
		public const string PUSH_NOTIFICATIONS_TOKENS_DIRECTORY = PUSH_NOTIFICATIONS_DIRECTORY + "/tokens";

		/// <summary>
		/// The push notifications requests directory.
		/// </summary>
		public const string PUSH_NOTIFICATIONS_REQUESTS_DIRECTORY = PUSH_NOTIFICATIONS_DIRECTORY + "/requests";

		/// <summary>
		/// The push notifications updates directory.
		/// </summary>
		public const string PUSH_NOTIFICATIONS_UPDATES_DIRECTORY = PUSH_NOTIFICATIONS_DIRECTORY + "/updates";

		/// <summary>
		/// The adaptive EMA policies directory.
		/// </summary>
		public const string ADAPTIVE_EMA_POLICIES_DIRECTORY = "adaptive-ema-policies";

		/// <summary>
		/// The sensing policies directory.
		/// </summary>
		public const string SENSING_POLICIES_DIRECTORY = "sensing-policies";

		private string _region;
		private string _bucket;
		private string _folder;
		private string _iamAccessKey;
		private string _iamSecretKey;
		private string _sessionToken;
		private string _pinnedServiceURL;
		private string _pinnedPublicKey;
		private int _putCount;
		private int _successfulPutCount;
		private bool _downloadingUpdates;
		private readonly object _downloadingUpdatesLocker = new object();

		/// <summary>
		/// The AWS region in which <see cref="Bucket"/> resides (e.g., us-east-2).
		/// </summary>
		/// <value>The region.</value>
		[ListUiProperty(null, true, 1, new object[] { "us-east-1", "us-east-2", "us-west-1", "us-west-2", "ca-central-1", "ap-south-1", "ap-northeast-2", "ap-southeast-1", "ap-southeast-2", "ap-northeast-1", "eu-central-1", "eu-west-1", "eu-west-2", "sa-east-1", "us-gov-west-1" }, true)]
		public string Region
		{
			get
			{
				return _region;
			}
			set
			{
				_region = value;
			}
		}

		/// <summary>
		/// The AWS S3 bucket in which data should be stored. This is the bucket identifier output by the steps described in the summary for this class.
		/// </summary>
		/// <value>The bucket.</value>
		[EntryStringUiProperty(null, true, 2, true)]
		public string Bucket
		{
			get
			{
				return _bucket;
			}
			set
			{
				if (value != null)
				{
					value = value.Trim().ToLower();  // bucket names must be lowercase.
				}

				_bucket = value;
			}
		}

		/// <summary>
		/// The folder within <see cref="Bucket"/> where data should be stored.
		/// </summary>
		/// <value>The folder.</value>
		[EntryStringUiProperty(null, true, 3, false)]
		public string Folder
		{
			get
			{
				return _folder;
			}
			set
			{
				if (value != null)
				{
					value = value.Trim().Trim('/');
				}

				_folder = value;
			}
		}

		/// <summary>
		/// The IAM user's access and secret keys output by the steps described in the summary for this class.
		/// </summary>
		/// <value>The iam account string.</value>
		[EntryStringUiProperty("IAM Account:", true, 4, true)]
		public string IamAccountString
		{
			get
			{
				return _iamAccessKey + ":" + _iamSecretKey + (string.IsNullOrWhiteSpace(_sessionToken) ? "" : ":" + _sessionToken);
			}
			set
			{
				bool validValue = false;

				if (!string.IsNullOrWhiteSpace(value))
				{
					string[] parts = value.Split(':');

					if (parts.Length == 2 || parts.Length == 3)
					{
						_iamAccessKey = parts[0].Trim();
						_iamSecretKey = parts[1].Trim();

						if (parts.Length == 3)
						{
							_sessionToken = parts[2].Trim();
						}

						validValue = true;
					}
				}

				if (!validValue)
				{
					_iamAccessKey = _iamSecretKey = null;
				}
			}
		}

		/// <summary>
		/// Alternative URL to use for S3, instead of the default. Use this to set up [SSL certificate pinning](xref:ssl_pinning).
		/// </summary>
		/// <value>The pinned service URL.</value>
		[EntryStringUiProperty("Pinned Service URL:", true, 5, false)]
		public string PinnedServiceURL
		{
			get
			{
				return _pinnedServiceURL;
			}
			set
			{
				if (value != null)
				{
					value = value.Trim();

					if (value == "")
					{
						value = null;
					}
					else
					{
						if (!value.ToLower().StartsWith("https://"))
						{
							value = "https://" + value;
						}
					}
				}

				_pinnedServiceURL = value;
			}
		}

		/// <summary>
		/// Pinned SSL public encryption key associated with <see cref="PinnedServiceURL"/>. Use this to set up [SSL certificate pinning](xref:ssl_pinning).
		/// </summary>
		/// <value>The pinned public key.</value>
		[EntryStringUiProperty("Pinned Public Key:", true, 6, false)]
		public string PinnedPublicKey
		{
			get
			{
				return _pinnedPublicKey;
			}
			set
			{
				_pinnedPublicKey = value?.Trim().Replace("\n", "").Replace(" ", "");
			}
		}

		[JsonIgnore]
		public override bool CanRetrieveWrittenData
		{
			get
			{
				return true;
			}
		}

		[JsonIgnore]
		public override string DisplayName
		{
			get
			{
				return "Amazon S3";
			}
		}

		public override string StorageDescription
		{
			get
			{
				return base.StorageDescription ?? "Data will be transmitted " + TimeSpan.FromMilliseconds(WriteDelayMS).GetFullDescription(TimeSpan.FromMilliseconds(DelayToleranceBeforeMS), TimeSpan.FromMilliseconds(DelayToleranceAfterMS)).ToLower() + ".";
			}
		}

		public AmazonS3RemoteDataStore()
		{
			_region = _bucket = _folder = null;
			_pinnedServiceURL = null;
			_pinnedPublicKey = null;
			_putCount = _successfulPutCount = 0;
		}

		public override async Task StartAsync()
		{
			// ensure that we have a pinned public key if we're pinning the service URL
			if (_pinnedServiceURL != null && string.IsNullOrWhiteSpace(_pinnedPublicKey))
			{
				throw new Exception("Ensure that a pinned public key is provided to the AWS S3 remote data store.");
			}
			else if (Protocol.AuthenticationService != null)
			{
				await GetCredentialsFromAuthenticationServiceAsync();
			}

			// credentials must have been set, either directly in the protocol or via the authentication service.
			if (string.IsNullOrWhiteSpace(_iamAccessKey) || string.IsNullOrWhiteSpace(_iamSecretKey))
			{
				throw new Exception("Must specify an IAM account within the S3 remote data store.");
			}

			// start base last so we're set up for any callbacks that get scheduled
			await base.StartAsync();
		}

		private async Task<AmazonS3Client> CreateS3ClientAsync()
		{
			if (Protocol.AuthenticationService != null)
			{
				await GetCredentialsFromAuthenticationServiceAsync();
			}

			AWSConfigs.LoggingConfig.LogMetrics = false;  // getting many uncaught exceptions from AWS S3 related to logging metrics
			AmazonS3Config clientConfig = new AmazonS3Config();

			if (_pinnedServiceURL == null)
			{
				clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(_region);
			}
			else
			{
				clientConfig.ServiceURL = _pinnedServiceURL;

				// when using pinning via CloudFront reverse proxy, the bucket name is prepended to the host if the path style is not used. the resulting host does not exist for our reverse proxy, causing DNS name resolution errors. by using the path style, the bucket is appended to the reverse-proxy host and everything goes through fine.
				clientConfig.ForcePathStyle = true;
			}

			AmazonS3Client client;

			if (string.IsNullOrWhiteSpace(_sessionToken))
			{
				client = new AmazonS3Client(_iamAccessKey, _iamSecretKey, clientConfig);
			}
			else
			{
				client = new AmazonS3Client(_iamAccessKey, _iamSecretKey, _sessionToken, clientConfig);
			}

			return client;
		}

		protected virtual string GetRemoteDirectory()
		{
			string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd");

			return DATA_DIRECTORY + "/" + (string.IsNullOrWhiteSpace(_folder) ? "" : _folder + "/") + timestamp + "/" + (string.IsNullOrWhiteSpace(Protocol.ParticipantId) ? "" : Protocol.ParticipantId + "/");
		}

		public override async Task WriteDataStreamAsync(Stream stream, string name, string contentType, CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				await PutAsync(s3, stream, GetRemoteDirectory() + name, contentType, cancellationToken);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		public override async Task WriteDatumAsync(Datum datum, CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();
				string datumJSON = datum.GetJSON(Protocol.JsonAnonymizer, true);
				byte[] datumJsonBytes = Encoding.UTF8.GetBytes(datumJSON);
				MemoryStream dataStream = new MemoryStream(datumJsonBytes);

				await PutAsync(s3, dataStream, GetDatumKey(datum), "application/json", cancellationToken);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		public override async Task SendPushNotificationTokenAsync(string token, CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				// get the token JSON payload
				PushNotificationRequestFormat localFormat = PushNotificationRequest.LocalFormat;
				string localFormatAzureIdentifier = PushNotificationRequest.GetAzureFormatIdentifier(localFormat);
				byte[] tokenBytes = Encoding.UTF8.GetBytes("{" +
															 "\"device\":" + JsonConvert.ToString(SensusServiceHelper.Get().DeviceId) + "," + 
															 "\"protocol\":" + JsonConvert.ToString(Protocol.Id) + "," + 
															 "\"format\":" + JsonConvert.ToString(localFormatAzureIdentifier) + "," +
															 "\"token\":" + JsonConvert.ToString(token) +
														   "}");
															
				MemoryStream dataStream = new MemoryStream(tokenBytes);

				await PutAsync(s3, dataStream, GetPushNotificationTokenKey(), "application/json", cancellationToken);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		public override async Task DeletePushNotificationTokenAsync(CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				await DeleteAsync(s3, GetPushNotificationTokenKey(), cancellationToken);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		private string GetPushNotificationTokenKey()
		{
			return PUSH_NOTIFICATIONS_TOKENS_DIRECTORY + "/" + SensusServiceHelper.Get().DeviceId + ".json";
		}

		public override async Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{

#if __IOS__
				// the current method is called in response to push notifications when starting the protocol after the app 
				// has been terminated. starting the protocol may involve a large number of callbacks, particularly for 
				// surveys scheduled many times into the future. sending the associated push notification requests can be 
				// very time consuming. we need to be sensitive to the alotted background execution time remaining so that
				// the protocol can start fully before background time expires. we don't want to run afoul of background 
				// execution time constraints. therefore we'll need to defer push notification request submission until the 
				// user foregrounds the app again and the health test submits the requests. the user will be encouraged to
				// do so as they'll begin getting notifications from the local callback invocation loop. in the check below
				// we use BackgroundTimeRemaining rather than ApplicationState, as we're unsure what the state will be when
				// the app is activated by a push notification. we're certain that background time will be limited. we're 
				// using a finite, relatively large value for the background time threshold to capture "in the background".
				// practical values are either the maximum floating point value (for foreground) and less than 30 seconds 
				// (for background).
				if (Protocol.State == ProtocolState.Starting && UIKit.UIApplication.SharedApplication.BackgroundTimeRemaining < 1000)
				{
					throw new Exception("Starting protocol from the background. Deferring submission of push notification requests.");
				}
#endif

				s3 = await CreateS3ClientAsync();
				byte[] requestJsonBytes = Encoding.UTF8.GetBytes(request.JSON.ToString(Formatting.None));
				MemoryStream dataStream = new MemoryStream(requestJsonBytes);

				await PutAsync(s3, dataStream, GetPushNotificationRequestKey(request.BackendKey), "application/json", cancellationToken);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		public override async Task DeletePushNotificationRequestAsync(Guid backendKey, CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				await DeleteAsync(s3, GetPushNotificationRequestKey(backendKey), cancellationToken);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		private string GetPushNotificationRequestKey(Guid backendKey)
		{
			return PUSH_NOTIFICATIONS_REQUESTS_DIRECTORY + "/" + backendKey + ".json";
		}

		private async Task PutAsync(AmazonS3Client s3, Stream stream, string key, string contentType, CancellationToken cancellationToken)
		{
			try
			{
				_putCount++;

				PutObjectRequest putRequest = new PutObjectRequest
				{
					BucketName = _bucket,
					CannedACL = S3CannedACL.BucketOwnerFullControl,  // without this, the bucket owner will not have access to the uploaded data
					InputStream = stream,
					Key = key,
					ContentType = contentType
				};

				HttpStatusCode putStatus = (await s3.PutObjectAsync(putRequest, cancellationToken)).HttpStatusCode;

				if (putStatus == HttpStatusCode.OK)
				{
					_successfulPutCount++;
				}
				else
				{
					throw new Exception(putStatus.ToString());
				}
			}
			catch (Exception ex)
			{
				string message = "Failed to write stream to Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message;
				SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
				throw new Exception(message, ex);
			}
		}

		private async Task<List<string>> ListKeysAsync(AmazonS3Client s3, string prefix, bool mostRecentlyModifiedFirst, CancellationToken cancellationToken)
		{
			try
			{
				List<string> keys = new List<string>();

				ListObjectsV2Request listRequest = new ListObjectsV2Request
				{
					BucketName = _bucket,
					MaxKeys = 100,
					Prefix = prefix
				};

				ListObjectsV2Response listResponse;

				do
				{
					listResponse = await s3.ListObjectsV2Async(listRequest, cancellationToken);

					if (listResponse.HttpStatusCode != HttpStatusCode.OK)
					{
						throw new Exception(listResponse.HttpStatusCode.ToString());
					}

					List<S3Object> s3Objects = listResponse.S3Objects;

					if (mostRecentlyModifiedFirst)
					{
						s3Objects.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
					}

					foreach (S3Object s3Object in s3Objects)
					{
						keys.Add(s3Object.Key);
					}

					listRequest.ContinuationToken = listResponse.NextContinuationToken;
				}
				while (listResponse.IsTruncated);

				return keys;
			}
			catch (Exception ex)
			{
				string message = "Failed to list keys in Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message;
				SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
				throw new Exception(message, ex);
			}
		}

		private async Task DeleteAsync(AmazonS3Client s3, string key, CancellationToken cancellationToken)
		{
			try
			{
				HttpStatusCode deleteStatus = (await s3.DeleteObjectAsync(_bucket, key, cancellationToken)).HttpStatusCode;

				if (deleteStatus != HttpStatusCode.NoContent)
				{
					throw new Exception(deleteStatus.ToString());
				}
			}
			catch (Exception ex)
			{
				string message = "Failed to delete key from Amazon S3 bucket \"" + _bucket + "\":  " + ex.Message;
				SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
				throw new Exception(message, ex);
			}
		}

		public override string GetDatumKey(Datum datum)
		{
			return GetRemoteDirectory() + datum.GetType().Name + "/" + datum.Id + ".json";
		}

		public override async Task<T> GetDatumAsync<T>(string datumKey, CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				Stream responseStream = (await s3.GetObjectAsync(_bucket, datumKey, cancellationToken)).ResponseStream;
				T datum = null;
				using (StreamReader reader = new StreamReader(responseStream))
				{
					string datumJSON = reader.ReadToEnd().Trim();
					datumJSON = SensusServiceHelper.Get().ConvertJsonForCrossPlatform(datumJSON);
					datum = Datum.FromJSON(datumJSON) as T;
				}

				return datum;
			}
			catch (Exception ex)
			{
				string message = "Failed to get datum from Amazon S3:  " + ex.Message;
				SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
				throw new Exception(message);
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		public override async Task<List<PushNotificationUpdate>> GetPushNotificationUpdatesAsync(CancellationToken cancellationToken)
		{
			Dictionary<string, PushNotificationUpdate> idUpdate = new Dictionary<string, PushNotificationUpdate>();

			// only let one caller at a time download the updates
			lock (_downloadingUpdatesLocker)
			{
				if (_downloadingUpdates)
				{
					throw new Exception("Push notification updates are being downloaded on another thread.");
				}
				else
				{
					_downloadingUpdates = true;
				}
			}

			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				// retrieve updates sorted with most recently modified first. this will let us keep only the most recent version
				// of each update with the same id (note use of TryAdd below, which will only retain the first update per id).
				foreach (string updateKey in await ListKeysAsync(s3, PUSH_NOTIFICATIONS_UPDATES_DIRECTORY + "/" + SensusServiceHelper.Get().DeviceId, true, cancellationToken))
				{
					try
					{
						GetObjectResponse getResponse = await s3.GetObjectAsync(_bucket, updateKey, cancellationToken);

						if (getResponse.HttpStatusCode == HttpStatusCode.OK)
						{
							// catch any exceptions when trying to delete the update. we already retrieved it, and 
							// we might just be lacking connectivity or might have been cancelled. the update will
							// be pushed again in the future and we'll try deleting it again then.
							try
							{
								await DeleteAsync(s3, updateKey, cancellationToken);
							}
							catch (Exception)
							{ }

							string updatesJSON;
							using (StreamReader reader = new StreamReader(getResponse.ResponseStream))
							{
								updatesJSON = reader.ReadToEnd().Trim();
							}

							foreach (JObject updateObject in JArray.Parse(updatesJSON))
							{
								PushNotificationUpdate update = updateObject.ToObject<PushNotificationUpdate>();
								idUpdate.TryAdd(update.Id, update);
							}
						}
						else
						{
							throw new Exception(getResponse.HttpStatusCode.ToString());
						}
					}
					catch (Exception ex)
					{
						SensusServiceHelper.Get().Logger.Log("Exception while getting update object:  " + ex.Message, LoggingLevel.Normal, GetType());
					}
				}

				SensusServiceHelper.Get().Logger.Log("Retrieved " + idUpdate.Count + " update(s).", LoggingLevel.Normal, GetType());

				return idUpdate.Values.ToList();
			}
			finally
			{
				lock (_downloadingUpdatesLocker)
				{
					_downloadingUpdates = false;
				}

				DisposeS3(s3);
			}
		}

		/// <summary>
		/// Gets the policy for the <see cref="Probes.User.Scripts.IScriptProbeAgent"/> from the current <see cref="AmazonS3RemoteDataStore"/>. 
		/// This method will download the policy JSON object from the following location:
		///  
		/// ```
		/// BUCKET/DIRECTORY/DEVICE
		/// ```
		/// 
		/// where `BUCKET` is <see cref="Bucket"/>, `DIRECTORY` is the value of <see cref="ADAPTIVE_EMA_POLICIES_DIRECTORY"/>, and
		/// `DEVICE` is the identifier of the current device as returned by <see cref="SensusServiceHelper.DeviceId"/>. This is the same device 
		/// identifier used within all <see cref="Datum"/> objects generated by the current device. This is also the same device identifier 
		/// stored in all JSON objects written to the <see cref="AmazonS3RemoteDataStore"/>. To provide a policy JSON object, write the policy 
		/// JSON object to the above S3 location. The content of this S3 location will be read, parsed as <see cref="JObject"/>, and delivered to the 
		/// <see cref="Probes.User.Scripts.IScriptProbeAgent"/> via <see cref="Probes.User.Scripts.IScriptProbeAgent.SetPolicyAsync(JObject)"/>.
		/// </summary>
		/// <returns>The script agent policy.</returns>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task<JObject> GetScriptAgentPolicyAsync(CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				Stream responseStream = (await s3.GetObjectAsync(_bucket, ADAPTIVE_EMA_POLICIES_DIRECTORY + "/" + SensusServiceHelper.Get().DeviceId, cancellationToken)).ResponseStream;

				JObject policy;
				using (StreamReader reader = new StreamReader(responseStream))
				{
					policy = JObject.Parse(reader.ReadToEnd().Trim());
				}

				return policy;
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		/// <summary>
		/// Gets the policy for the <see cref="Protocol.Agent"/> from the current <see cref="AmazonS3RemoteDataStore"/>. 
		/// This method will download the policy JSON object from the following location:
		///  
		/// ```
		/// BUCKET/DIRECTORY/DEVICE
		/// ```
		/// 
		/// where `BUCKET` is <see cref="Bucket"/>, `DIRECTORY` is the value of <see cref="SENSING_POLICIES_DIRECTORY"/>, and
		/// `DEVICE` is the identifier of the current device as returned by <see cref="SensusServiceHelper.DeviceId"/>. This is the same device 
		/// identifier used within all <see cref="Datum"/> objects generated by the current device. This is also the same device identifier 
		/// stored in all JSON objects written to the <see cref="AmazonS3RemoteDataStore"/>. To provide a policy JSON object, write the policy 
		/// JSON object to the above S3 location. The content of this S3 location will be read, parsed as <see cref="JObject"/>, and delivered to the 
		/// <see cref="Protocol.Agent"/> via <see cref="Adaptation.SensingAgent.SetPolicyAsync(JObject)"/>.
		/// </summary>
		/// <returns>The sensing agent policy.</returns>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task<JObject> GetSensingAgentPolicyAsync(CancellationToken cancellationToken)
		{
			AmazonS3Client s3 = null;

			try
			{
				s3 = await CreateS3ClientAsync();

				Stream responseStream = (await s3.GetObjectAsync(_bucket, SENSING_POLICIES_DIRECTORY + "/" + SensusServiceHelper.Get().DeviceId, cancellationToken)).ResponseStream;

				JObject policy;
				using (StreamReader reader = new StreamReader(responseStream))
				{
					policy = JObject.Parse(reader.ReadToEnd().Trim());
				}

				return policy;
			}
			finally
			{
				DisposeS3(s3);
			}
		}

		private void DisposeS3(AmazonS3Client s3)
		{
			if (s3 != null)
			{
				try
				{
					s3.Dispose();
				}
				catch (Exception ex)
				{
					SensusServiceHelper.Get().Logger.Log("Failed to dispose Amazon S3 client:  " + ex.Message, LoggingLevel.Normal, GetType());
				}
			}
		}

		private async Task GetCredentialsFromAuthenticationServiceAsync()
		{
			if (Protocol.AuthenticationService == null)
			{
				SensusException.Report(nameof(GetCredentialsFromAuthenticationServiceAsync) + " called without an authentication service.");
			}
			else
			{
				// set keys/token for use in the data store
				AmazonS3Credentials s3Credentials = await Protocol.AuthenticationService.GetCredentialsAsync();
				_iamAccessKey = s3Credentials.AccessKeyId;
				_iamSecretKey = s3Credentials.SecretAccessKey;
				_sessionToken = s3Credentials.SessionToken;
				_region = s3Credentials.Region;
			}
		}

		public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
		{
			HealthTestResult result = await base.TestHealthAsync(events);

			string eventName = TrackedEvent.Health + ":" + GetType().Name;
			Dictionary<string, string> properties = new Dictionary<string, string>
			{
				{ "Put Success", Convert.ToString(_successfulPutCount.RoundToWholePercentageOf(_putCount, 5)) }
			};

			events.Add(new AnalyticsTrackedEvent(eventName, properties));

			return result;
		}

		public override void Reset()
		{
			base.Reset();

			// session tokens are not meant to persist across instantiations of the protocol.
			_sessionToken = null;
		}
	}
}
