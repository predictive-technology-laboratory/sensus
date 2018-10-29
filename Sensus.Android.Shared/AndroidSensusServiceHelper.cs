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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Speech;
using Android.Support.V4.Content;
using Android.Widget;
using Newtonsoft.Json;
using Sensus.Probes.Location;
using Sensus.Probes;
using Sensus.Probes.Movement;
using System.Linq;
using ZXing.Mobile;
using Android.Graphics;
using Android.Bluetooth;
using Android.Hardware;
using Sensus.Android.Probes.Context;
using System.Threading.Tasks;
using Sensus.Context;
using Firebase.Iid;
using Sensus.Exceptions;
using WindowsAzure.Messaging;
using Sensus.UI;
using Xamarin.Forms;

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        private AndroidSensusService _service;
        private string _deviceId;
        private AndroidMainActivity _focusedMainActivity;
        private readonly object _focusedMainActivityLocker = new object();
        private PowerManager.WakeLock _wakeLock;
        private int _wakeLockAcquisitionCount;
        private List<Action<AndroidMainActivity>> _actionsToRunUsingMainActivity;
        private bool _userDeniedBluetoothEnable;

        public override string DeviceId
        {
            get { return _deviceId; }
        }

        public override string DeviceManufacturer
        {
            get { return Build.Manufacturer; }
        }

        public override string DeviceModel
        {
            get { return Build.Device; }
        }

        public override bool WiFiConnected
        {
            get
            {
                ConnectivityManager connectivityManager = _service.GetSystemService(global::Android.Content.Context.ConnectivityService) as ConnectivityManager;

                if (connectivityManager == null)
                {
                    Logger.Log("No connectivity manager available for WiFi check.", LoggingLevel.Normal, GetType());
                    return false;
                }

                // see the Backwards Compatibility article for more information
#if __ANDROID_21__
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    foreach (Network network in connectivityManager.GetAllNetworks())
                    {
                        // we've seen NREs in the following lines. 
                        if ((connectivityManager.GetNetworkInfo(network)?.IsConnected ?? false) && 
                            (connectivityManager.GetNetworkCapabilities(network)?.HasTransport(TransportType.Wifi) ?? false))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                else
#endif
                {
                    // ignore deprecation warning
#pragma warning disable 618
                    return connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).IsConnected;
#pragma warning restore 618
                }
            }
        }

        public override bool IsCharging
        {
            get
            {
                IntentFilter filter = new IntentFilter(Intent.ActionBatteryChanged);
                BatteryStatus status = (BatteryStatus)_service.RegisterReceiver(null, filter).GetIntExtra(BatteryManager.ExtraStatus, -1);
                return status == BatteryStatus.Charging || status == BatteryStatus.Full;
            }
        }

        public override float BatteryChargePercent
        {
            get
            {
                Intent batteryIntent = Application.Context.RegisterReceiver(null, new IntentFilter(Intent.ActionBatteryChanged));

                if (batteryIntent == null)
                {
                    throw new Exception("Failed to poll battery status.");
                }
                else
                {
                    float level = batteryIntent.GetIntExtra(BatteryManager.ExtraLevel, -1);
                    float scale = batteryIntent.GetIntExtra(BatteryManager.ExtraScale, -1);

                    if (level >= 0 && scale >= 0)
                    {
                        return 100 * level / scale;
                    }
                    else
                    {
                        throw new Exception("Failed to obtain battery charge percent. Level or scale <= 0.");
                    }
                }
            }
        }

        public override string OperatingSystem
        {
            get
            {
                return "Android " + Build.VERSION.SdkInt;
            }
        }

        protected override bool IsOnMainThread
        {
            get
            {
                // we should always have a service. if we do not, assume the worst -- that we're on the main thread. this will hopefully
                // produce an error report back at xamarin insights.
                if (_service == null)
                {
                    return true;
                }
                // if we have a service, compare the current thread's looper to the main thread's looper
                else
                {
                    return Looper.MyLooper() == _service.MainLooper;
                }
            }
        }

        public override string Version
        {
            get
            {
                return _service?.PackageManager.GetPackageInfo(_service.PackageName, PackageInfoFlags.Activities).VersionName ?? null;
            }
        }

        [JsonIgnore]
        public int WakeLockAcquisitionCount
        {
            get { return _wakeLockAcquisitionCount; }
        }

        public bool UserDeniedBluetoothEnable
        {
            get
            {
                return _userDeniedBluetoothEnable;
            }

            set
            {
                _userDeniedBluetoothEnable = value;
            }
        }

        public override string PushNotificationToken
        {
            get
            {
                return FirebaseInstanceId.Instance.Token;
            }
        }

        public AndroidSensusServiceHelper()
        {
            _actionsToRunUsingMainActivity = new List<Action<AndroidMainActivity>>();
            _userDeniedBluetoothEnable = false;
        }

        public void SetService(AndroidSensusService service)
        {
            _service = service;

            if (_service == null)
            {
                if (_wakeLock != null)
                {
                    _wakeLock.Dispose();
                    _wakeLock = null;
                }
            }
            else
            {
                _wakeLock = (_service.GetSystemService(global::Android.Content.Context.PowerService) as PowerManager).NewWakeLock(WakeLockFlags.Partial, "SENSUS");
                _wakeLockAcquisitionCount = 0;
                _deviceId = Settings.Secure.GetString(_service.ContentResolver, Settings.Secure.AndroidId);
            }
        }

        #region main activity

        /// <summary>
        /// Runs an action using main activity, optionally bringing the main activity into focus if it is not already focused.
        /// </summary>
        /// <param name="action">Action to run.</param>
        /// <param name="startMainActivityIfNotFocused">Whether or not to start the main activity if it is not currently focused.</param>
        /// <param name="holdActionIfNoActivity">If the main activity is not focused and we're not starting a new one to refocus it, whether 
        /// or not to hold the action for later when the activity is refocused.</param>
        public Task RunActionUsingMainActivityAsync(Action<AndroidMainActivity> action, bool startMainActivityIfNotFocused, bool holdActionIfNoActivity)
        {
            return Task.Run(() =>
            {
                lock (_focusedMainActivityLocker)
                {
                    // run actions now only if the main activity is focused. this is a stronger requirement than merely started/resumed since it
                    // implies that the user interface is up. this is important because if certain actions (e.g., speech recognition) are run
                    // after the activity is resumed but before the window is up, the appearance of the activity's window can hide/cancel the
                    // action's window.
                    if (_focusedMainActivity == null)
                    {
                        if (startMainActivityIfNotFocused)
                        {
                            // we'll run the action when the activity is focused
                            lock (_actionsToRunUsingMainActivity)
                            {
                                _actionsToRunUsingMainActivity.Add(action);
                            }

                            Logger.Log("Starting main activity to run action.", LoggingLevel.Normal, GetType());

                            // start the activity. when it starts, it will call back to SetFocusedMainActivity indicating readiness. once 
                            // this happens, we'll be ready to run the action that was just passed in as well as any others that need to be run.
                            Intent intent = new Intent(_service, typeof(AndroidMainActivity));
                            intent.AddFlags(ActivityFlags.FromBackground | ActivityFlags.NewTask);
                            _service.StartActivity(intent);
                        }
                        else if (holdActionIfNoActivity)
                        {
                            // we'll run the action the next time the activity is focused
                            lock (_actionsToRunUsingMainActivity)
                            {
                                _actionsToRunUsingMainActivity.Add(action);
                            }
                        }
                    }
                    else
                    {
                        // we'll run the action now
                        lock (_actionsToRunUsingMainActivity)
                        {
                            _actionsToRunUsingMainActivity.Add(action);
                        }

                        RunActionsUsingMainActivity();
                    }
                }
            });
        }

        public void SetFocusedMainActivity(AndroidMainActivity focusedMainActivity)
        {
            lock (_focusedMainActivityLocker)
            {
                _focusedMainActivity = focusedMainActivity;

                if (_focusedMainActivity == null)
                {
                    Logger.Log("Main activity not focused.", LoggingLevel.Normal, GetType());
                }
                else
                {
                    Logger.Log("Main activity focused.", LoggingLevel.Normal, GetType());
                    RunActionsUsingMainActivity();
                }
            }
        }

        private void RunActionsUsingMainActivity()
        {
            lock (_focusedMainActivityLocker)
            {
                lock (_actionsToRunUsingMainActivity)
                {
                    Logger.Log("Running " + _actionsToRunUsingMainActivity.Count + " actions using main activity.", LoggingLevel.Debug, GetType());

                    foreach (Action<AndroidMainActivity> action in _actionsToRunUsingMainActivity)
                    {
                        action(_focusedMainActivity);
                    }

                    _actionsToRunUsingMainActivity.Clear();
                }
            }
        }

        #endregion

        #region miscellaneous platform-specific functions
        public override async Task PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            try
            {
                Intent intent = new Intent(Intent.ActionGetContent);
                intent.SetType("*/*");
                intent.AddCategory(Intent.CategoryOpenable);

                await RunActionUsingMainActivityAsync(mainActivity =>
                {
                    mainActivity.GetActivityResultAsync(intent, AndroidActivityResultRequestCode.PromptForFile, async result =>
                    {
                        if (result != null && result.Item1 == Result.Ok)
                        {
                            try
                            {
                                using (StreamReader file = new StreamReader(_service.ContentResolver.OpenInputStream(result.Item2.Data)))
                                {
                                    callback(file.ReadToEnd());
                                }
                            }
                            catch (Exception ex)
                            {
                                await FlashNotificationAsync("Error reading text file:  " + ex.Message);
                            }
                        }
                    });

                }, true, false);
            }
            catch (ActivityNotFoundException)
            {
                await FlashNotificationAsync("Please install a file manager from the Apps store.");
            }
            catch (Exception ex)
            {
                await FlashNotificationAsync("Something went wrong while prompting you for a file to read:  " + ex.Message);
            }
        }

        public override async Task ShareFileAsync(string path, string subject, string mimeType)
        {
            try
            {
                // run from main activity to get a smoother transition back to sensus
                await RunActionUsingMainActivityAsync(async mainActivity =>
                {
                    // make file available via external storage
                    Page page = (mainActivity.Application as App).DetailPage;
                    if (await page.DisplayAlert("USB or App?", "Would you like to share via USB or via another app?", "USB", "Another app"))
                    {
                        string externalPath = Path.Combine(global::Android.OS.Environment.ExternalStorageDirectory, Guid.NewGuid().ToString());
                        File.Move(path, externalPath);
                        await page.DisplayAlert("Done", "File available via USB:  " + Path.GetFileName(externalPath), "OK");
                    }
                    // share via app
                    else
                    {
                        Intent intent = new Intent(Intent.ActionSend);
                        intent.SetType(mimeType);
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                        if (!string.IsNullOrWhiteSpace(subject))
                        {
                            intent.PutExtra(Intent.ExtraSubject, subject);
                        }

                        Java.IO.File file = new Java.IO.File(path);
                        global::Android.Net.Uri uri = FileProvider.GetUriForFile(_service, "edu.virginia.sie.ptl.sensus.fileprovider", file);
                        intent.PutExtra(Intent.ExtraStream, uri);

                        mainActivity.StartActivity(intent);
                    }

                }, true, false);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to start intent to share file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        public override Task SendEmailAsync(string toAddress, string subject, string message)
        {
            return RunActionUsingMainActivityAsync(mainActivity =>
            {
                Intent emailIntent = new Intent(Intent.ActionSend);
                emailIntent.PutExtra(Intent.ExtraEmail, new string[] { toAddress });
                emailIntent.PutExtra(Intent.ExtraSubject, subject);
                emailIntent.PutExtra(Intent.ExtraText, message);
                emailIntent.SetType("text/plain");

                mainActivity.StartActivity(emailIntent);

            }, true, false);
        }

        public override async Task TextToSpeechAsync(string text)
        {
            AndroidTextToSpeech textToSpeech = new AndroidTextToSpeech(_service);
            await textToSpeech.SpeakAsync(text);
            textToSpeech.Dispose();
        }

        public override async Task<string> RunVoicePromptAsync(string prompt, Action postDisplayCallback)
        {
            string input = null;
            ManualResetEvent dialogDismissWait = new ManualResetEvent(false);

            await RunActionUsingMainActivityAsync(mainActivity =>
            {
                mainActivity.RunOnUiThread(() =>
                {
                    #region set up dialog
                    TextView promptView = new TextView(mainActivity) { Text = prompt, TextSize = 20 };
                    EditText inputEdit = new EditText(mainActivity) { TextSize = 20 };
                    LinearLayout scrollLayout = new LinearLayout(mainActivity) { Orientation = global::Android.Widget.Orientation.Vertical };
                    scrollLayout.AddView(promptView);
                    scrollLayout.AddView(inputEdit);
                    ScrollView scrollView = new ScrollView(mainActivity);
                    scrollView.AddView(scrollLayout);

                    AlertDialog dialog = new AlertDialog.Builder(mainActivity)
                        .SetTitle("Sensus is requesting input...")
                        .SetView(scrollView)
                        .SetPositiveButton("OK", (o, e) =>
                        {
                            input = inputEdit.Text;
                        })
                        .SetNegativeButton("Cancel", (o, e) =>
                        {
                        })
                        .Create();

                    dialog.DismissEvent += (o, e) =>
                    {
                        dialogDismissWait.Set();
                    };

                    ManualResetEvent dialogShowWait = new ManualResetEvent(false);

                    dialog.ShowEvent += (o, e) =>
                    {
                        dialogShowWait.Set();
                        postDisplayCallback?.Invoke();
                    };

                    // dismiss the keyguard when dialog appears
                    dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.DismissKeyguard);
                    dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked);
                    dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.TurnScreenOn);
                    dialog.Window.SetSoftInputMode(global::Android.Views.SoftInput.AdjustResize | global::Android.Views.SoftInput.StateAlwaysHidden);

                    // dim whatever is behind the dialog
                    dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.DimBehind);
                    dialog.Window.Attributes.DimAmount = 0.75f;

                    dialog.Show();
                    #endregion

                    #region voice recognizer
                    Task.Run(() =>
                    {
                        // wait for the dialog to be shown so it doesn't hide our speech recognizer activity
                        dialogShowWait.WaitOne();

                        // there's a slight race condition between the dialog showing and speech recognition showing. pause here to prevent the dialog from hiding the speech recognizer.
                        Thread.Sleep(1000);

                        Intent intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                        intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                        intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
                        intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
                        intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 15000);
                        intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                        intent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);
                        intent.PutExtra(RecognizerIntent.ExtraPrompt, prompt);

                        mainActivity.GetActivityResultAsync(intent, AndroidActivityResultRequestCode.RecognizeSpeech, result =>
                        {
                            if (result != null && result.Item1 == Result.Ok)
                            {
                                IList<string> matches = result.Item2.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                                if (matches != null && matches.Count > 0)
                                {
                                    mainActivity.RunOnUiThread(() =>
                                    {
                                        inputEdit.Text = matches[0];
                                    });
                                }
                            }
                        });
                    });
                    #endregion
                });

            }, true, false);

            dialogDismissWait.WaitOne();

            return input;
        }

        #endregion

        protected override Task ProtectedFlashNotificationAsync(string message)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                Toast.MakeText(Application.Context, message, ToastLength.Long).Show();
            });

            return Task.CompletedTask;
        }

        public override bool EnableProbeWhenEnablingAll(Probe probe)
        {
            // listening for locations doesn't work very well in android, since it conflicts with polling and uses more power. don't enable probes that need location listening by default.
            return !(probe is ListeningLocationProbe) &&
            !(probe is ListeningSpeedProbe) &&
            !(probe is ListeningPointsOfInterestProximityProbe);
        }

        public override Xamarin.Forms.ImageSource GetQrCodeImageSource(string contents)
        {
            return Xamarin.Forms.ImageSource.FromStream(() =>
            {
                Bitmap bitmap = BarcodeWriter.Write(contents);
                MemoryStream ms = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Png, 100, ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            });
        }

        protected override Task RegisterWithNotificationHubAsync(Tuple<string, string> hubSas)
        {
            // cannot perform registration on main thread. ensure we're on another thread.
            return Task.Run(() =>
            {
                NotificationHub notificationHub = new NotificationHub(hubSas.Item1, hubSas.Item2, Application.Context);
                notificationHub.Register(PushNotificationToken);
            });
        }

        protected override Task UnregisterFromNotificationHubAsync(Tuple<string, string> hubSas)
        {
            // cannot perform registration on main thread. ensure we're on another thread.
            return Task.Run(() =>
            {
                NotificationHub notificationHub = new NotificationHub(hubSas.Item1, hubSas.Item2, Application.Context);
                notificationHub.UnregisterAll(PushNotificationToken);
            });
        }

        protected override void RequestNewPushNotificationToken()
        {
            // delete the instance ID and get a new token.
            FirebaseInstanceId.Instance.DeleteInstanceId();
            string x = FirebaseInstanceId.Instance.Token;  // this will force the acquisition of a new token.
        }

        /// <summary>
        /// Enables the Bluetooth adapter, or prompts the user to do so if we cannot do this programmatically.
        /// </summary>
        /// <returns><c>true</c>, if Bluetooth was enabled, <c>false</c> otherwise.</returns>
        /// <param name="lowEnergy">If set to <c>true</c> low energy.</param>
        /// <param name="rationale">Rationale.</param>
        public override async Task<bool> EnableBluetoothAsync(bool lowEnergy, string rationale)
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

            // ensure that the device has the required feature
            if (bluetoothAdapter == null || !_service.PackageManager.HasSystemFeature(lowEnergy ? PackageManager.FeatureBluetoothLe : PackageManager.FeatureBluetooth))
            {
                await FlashNotificationAsync("This device does not have Bluetooth " + (lowEnergy ? "Low Energy" : "") + ".");
                return false;
            }

            // the system has bluetooth. check whether it's enabled.
            if (bluetoothAdapter.IsEnabled)
            {
                return true;
            }
            else
            {
                // if it's not and if the user has previously denied bluetooth, quit now. don't bother the user again.
                if (_userDeniedBluetoothEnable)
                {
                    return false;
                }
                else
                {
                    TaskCompletionSource<bool> enableTaskCompletionSource = new TaskCompletionSource<bool>();

                    // bring up sensus so we can request bluetooth enable
                    await RunActionUsingMainActivityAsync(mainActivity =>
                    {
                        mainActivity.RunOnUiThread(async () =>
                        {
                            try
                            {
                                // explain why we need bluetooth
                                await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Bluetooth", "Sensus will now prompt you to enable Bluetooth. " + rationale, "OK");

                                // prompt for permission
                                Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                                mainActivity.GetActivityResultAsync(enableIntent, AndroidActivityResultRequestCode.EnableBluetooth, resultIntent =>
                                {
                                    if (resultIntent.Item1 == Result.Canceled)
                                    {
                                        _userDeniedBluetoothEnable = true;
                                        enableTaskCompletionSource.TrySetResult(false);
                                    }
                                    else if (resultIntent.Item1 == Result.Ok)
                                    {
                                        enableTaskCompletionSource.TrySetResult(true);
                                    }
                                    else
                                    {
                                        enableTaskCompletionSource.TrySetResult(false);
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger.Log("Failed to start Bluetooth:  " + ex.Message, LoggingLevel.Normal, GetType());
                                enableTaskCompletionSource.TrySetResult(false);
                            }
                        });

                    }, true, false);

                    bool enabled = await enableTaskCompletionSource.Task;

                    if (enabled)
                    {
                        // the user enabled bluetooth, so allow one retry at enabling next time we find BLE disabled
                        _userDeniedBluetoothEnable = false;
                    }

                    return enabled;
                }
            }
        }

        public override async Task<bool> DisableBluetoothAsync(bool reenable, bool lowEnergy = true, string rationale = null)
        {
            BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

            // check whether bluetooth is enabled
            if (bluetoothAdapter?.IsEnabled ?? false)
            {
                TaskCompletionSource<State> bluetoothStateChangedCompletionSource = new TaskCompletionSource<State>();

                EventHandler<State> StateChangedHandler = (sender, newState) =>
                {
                    if (newState == State.On || newState == State.Off)
                    {
                        bluetoothStateChangedCompletionSource.TrySetResult(newState);
                    }
                };

                AndroidBluetoothBroadcastReceiver.STATE_CHANGED += StateChangedHandler;

                try
                {
                    if (!bluetoothAdapter.Disable())
                    {
                        bluetoothStateChangedCompletionSource.TrySetResult(State.On);
                    }
                }
                catch (Exception)
                {
                    bluetoothStateChangedCompletionSource.TrySetResult(State.On);
                }

                await bluetoothStateChangedCompletionSource.Task;

                // try to reenable without user interaction -- may not be allowed.
                if (reenable)
                {
                    bluetoothStateChangedCompletionSource = new TaskCompletionSource<State>();

                    try
                    {
                        if (!bluetoothAdapter.Enable())
                        {
                            bluetoothStateChangedCompletionSource.TrySetResult(State.Off);
                        }
                    }
                    catch (Exception)
                    {
                        bluetoothStateChangedCompletionSource.TrySetResult(State.Off);
                    }

                    await bluetoothStateChangedCompletionSource.Task;
                }

                AndroidBluetoothBroadcastReceiver.STATE_CHANGED -= StateChangedHandler;
            }

            bool isEnabled = bluetoothAdapter?.IsEnabled ?? false;

            // dispatch an intent to reenable bluetooth, which will require user interaction.
            if (reenable && !isEnabled)
            {
                return await EnableBluetoothAsync(lowEnergy, rationale);
            }
            else
            {
                return isEnabled;
            }
        }

        #region device awake / sleep

        public override void KeepDeviceAwake()
        {
            if (_wakeLock != null)
            {
                lock (_wakeLock)
                {
                    _wakeLock.Acquire();
                    Logger.Log("Wake lock acquisition count:  " + ++_wakeLockAcquisitionCount, LoggingLevel.Verbose, GetType());
                }
            }
        }

        public override void LetDeviceSleep()
        {
            if (_wakeLock != null)
            {
                lock (_wakeLock)
                {
                    _wakeLock.Release();
                    Logger.Log("Wake lock acquisition count:  " + --_wakeLockAcquisitionCount, LoggingLevel.Verbose, GetType());
                }
            }
        }

        /// <summary>
        /// Brings the Sensus UI to the foreground.
        /// </summary>
        public override Task BringToForegroundAsync()
        {
            return RunActionUsingMainActivityAsync(activity => { }, true, false);
        }

        #endregion

        public void ReissueForegroundServiceNotification()
        {
            _service.ReissueForegroundServiceNotification();
        }

        public void StopAndroidSensusService()
        {
            _service.Stop();
        }

        public SensorManager GetSensorManager()
        {
            return _service.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
        }
    }
}