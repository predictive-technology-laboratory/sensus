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
using SensusService;
using Xamarin;
using SensusService.Probes.Location;
using SensusService.Probes;
using SensusService.Probes.Movement;
using System.Linq;
using ZXing.Mobile;
using Android.Graphics;
using Android.Media;
using Android.Bluetooth;

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        private AndroidSensusService _service;
        private ConnectivityManager _connectivityManager;
        private NotificationManager _notificationManager;
        private string _deviceId;
        private AndroidMainActivity _focusedMainActivity;
        private readonly object _focusedMainActivityLocker = new object();
        private AndroidTextToSpeech _textToSpeech;
        private PowerManager.WakeLock _wakeLock;
        private int _wakeLockAcquisitionCount;
        private List<Action<AndroidMainActivity>> _actionsToRunUsingMainActivity;
        private Dictionary<string, PendingIntent> _callbackIdPendingIntent;
        private bool _userDeniedBluetoothEnable;

        [JsonIgnore]
        public AndroidSensusService Service
        {
            get { return _service; }
        }

        public override string DeviceId
        {
            get { return _deviceId; }
        }

        public override bool WiFiConnected
        {
            get
            {
                // https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
#if __ANDROID_21__
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    return _connectivityManager.GetAllNetworks().Select(network => _connectivityManager.GetNetworkInfo(network)).Any(networkInfo => networkInfo != null && networkInfo.Type == ConnectivityType.Wifi && networkInfo.IsConnected);  // API level 21
                else
#endif
                {
                    // ignore deprecation warning
#pragma warning disable 618
                    return _connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).IsConnected;
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
                    return true;
                // if we have a service, compare the current thread's looper to the main thread's looper
                else
                    return Looper.MyLooper() == _service.MainLooper;
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

        public AndroidSensusServiceHelper()
        {
            _actionsToRunUsingMainActivity = new List<Action<AndroidMainActivity>>();
            _callbackIdPendingIntent = new Dictionary<string, PendingIntent>();
            _userDeniedBluetoothEnable = false;
        }

        public void SetService(AndroidSensusService service)
        {
            _service = service;

            if (_service == null)
            {
                if (_connectivityManager != null)
                {
                    _connectivityManager.Dispose();
                    _connectivityManager = null;
                }

                if (_notificationManager != null)
                {
                    _notificationManager.Dispose();
                    _notificationManager = null;
                }

                if (_textToSpeech != null)
                {
                    _textToSpeech.Dispose();
                    _textToSpeech = null;
                }

                if (_wakeLock != null)
                {
                    _wakeLock.Dispose();
                    _wakeLock = null;
                }
            }
            else
            {
                _connectivityManager = _service.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                _notificationManager = _service.GetSystemService(Context.NotificationService) as NotificationManager;
                _textToSpeech = new AndroidTextToSpeech(_service);
                _wakeLock = (_service.GetSystemService(Context.PowerService) as PowerManager).NewWakeLock(WakeLockFlags.Partial, "SENSUS");
                _wakeLockAcquisitionCount = 0;
                _deviceId = Settings.Secure.GetString(_service.ContentResolver, Settings.Secure.AndroidId);

                // must initialize after _deviceId is set
                if (Insights.IsInitialized)
                    Insights.Identify(_deviceId, "Device ID", _deviceId);
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
        public void RunActionUsingMainActivityAsync(Action<AndroidMainActivity> action, bool startMainActivityIfNotFocused, bool holdActionIfNoActivity)
        {
            // this must be done asynchronously because it blocks waiting for the activity to start. calling this method from the UI would create deadlocks.
            new Thread(() =>
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
                                    _actionsToRunUsingMainActivity.Add(action);

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
                                    _actionsToRunUsingMainActivity.Add(action);
                            }
                        }
                        else
                        {
                            // we'll run the action now
                            lock (_actionsToRunUsingMainActivity)
                                _actionsToRunUsingMainActivity.Add(action);

                            RunActionsUsingMainActivity();
                        }
                    }

                }).Start();
        }

        public void SetFocusedMainActivity(AndroidMainActivity focusedMainActivity)
        {
            lock (_focusedMainActivityLocker)
            {
                _focusedMainActivity = focusedMainActivity;

                if (_focusedMainActivity == null)
                    Logger.Log("Main activity not focused.", LoggingLevel.Normal, GetType());
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
                        action(_focusedMainActivity);

                    _actionsToRunUsingMainActivity.Clear();
                }
            }
        }

        #endregion

        #region miscellaneous platform-specific functions

        protected override void InitializeXamarinInsights()
        {
            Insights.Initialize(XAMARIN_INSIGHTS_APP_KEY, Application.Context);  // can't reference _service here since this method is called from the base class constructor, before the service is set.
        }

        public override void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            new Thread(() =>
            {
                try
                {
                    Intent intent = new Intent(Intent.ActionGetContent);
                    intent.SetType("*/*");
                    intent.AddCategory(Intent.CategoryOpenable);

                    RunActionUsingMainActivityAsync(mainActivity =>
                    {
                        mainActivity.GetActivityResultAsync(intent, AndroidActivityResultRequestCode.PromptForFile, result =>
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
                                    FlashNotificationAsync("Error reading text file:  " + ex.Message);
                                }
                            }
                        });

                    }, true, false);
                }
                catch (ActivityNotFoundException)
                {
                    FlashNotificationAsync("Please install a file manager from the Apps store.");
                }
                catch (Exception ex)
                {
                    FlashNotificationAsync("Something went wrong while prompting you for a file to read:  " + ex.Message);
                }

            }).Start();
        }

        public override void ShareFileAsync(string path, string subject, string mimeType)
        {
            new Thread(() =>
                {
                    try
                    {
                        Intent intent = new Intent(Intent.ActionSend);
                        intent.SetType(mimeType);
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                        if (!string.IsNullOrWhiteSpace(subject))
                            intent.PutExtra(Intent.ExtraSubject, subject);

                        Java.IO.File file = new Java.IO.File(path);
                        global::Android.Net.Uri uri = FileProvider.GetUriForFile(_service, "edu.virginia.sie.ptl.sensus.fileprovider", file);
                        intent.PutExtra(Intent.ExtraStream, uri);

                        // run from main activity to get a smoother transition back to sensus
                        RunActionUsingMainActivityAsync(mainActivity =>
                            {
                                mainActivity.StartActivity(intent);

                            }, true, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Failed to start intent to share file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                    }

                }).Start();
        }

        public override void SendEmailAsync(string toAddress, string subject, string message)
        {
            RunActionUsingMainActivityAsync(mainActivity =>
            {
                Intent emailIntent = new Intent(Intent.ActionSend);
                emailIntent.PutExtra(Intent.ExtraEmail, new string[] { toAddress });
                emailIntent.PutExtra(Intent.ExtraSubject, subject);
                emailIntent.PutExtra(Intent.ExtraText, message);
                emailIntent.SetType("text/plain");

                mainActivity.StartActivity(emailIntent);

            }, true, false);
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            _textToSpeech.SpeakAsync(text, callback);
        }

        public override void RunVoicePromptAsync(string prompt, Action postDisplayCallback, Action<string> callback)
        {
            new Thread(() =>
                {
                    string input = null;
                    ManualResetEvent dialogDismissWait = new ManualResetEvent(false);

                    RunActionUsingMainActivityAsync(mainActivity =>
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

                                        if (postDisplayCallback != null)
                                            postDisplayCallback();
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
                                    new Thread(() =>
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
                                                            mainActivity.RunOnUiThread(() =>
                                                                {
                                                                    inputEdit.Text = matches[0];
                                                                });
                                                    }
                                                });

                                        }).Start();
                                    #endregion
                                });

                        }, true, false);

                    dialogDismissWait.WaitOne();
                    callback(input);

                }).Start();
        }

        #endregion

        #region notifications

        public override void IssueNotificationAsync(string message, string id, bool playSound, bool vibrate)
        {
            IssueNotificationAsync("Sensus", message, true, false, id, playSound, vibrate);
        }

        public void IssueNotificationAsync(string title, string message, bool autoCancel, bool ongoing, string id, bool playSound, bool vibrate)
        {
            if (_notificationManager != null)
            {
                new Thread(() =>
                {
                    if (message == null)
                        _notificationManager.Cancel(id, 0);
                    else
                    {
                        Intent serviceIntent = new Intent(_service, typeof(AndroidSensusService));
                        serviceIntent.PutExtra(NOTIFICATION_ID_KEY, id);
                        PendingIntent pendingIntent = PendingIntent.GetService(_service, 0, serviceIntent, PendingIntentFlags.UpdateCurrent);

                        Notification.Builder builder = new Notification.Builder(_service)
                            .SetContentTitle(title)
                            .SetContentText(message)
                            .SetSmallIcon(Resource.Drawable.ic_launcher)
                            .SetContentIntent(pendingIntent)
                            .SetAutoCancel(autoCancel)
                            .SetOngoing(ongoing);

                        if (playSound)
                            builder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));

                        if (vibrate)
                            builder.SetVibrate(new long[] { 0, 250, 50, 250 });

                        _notificationManager.Notify(id, 0, builder.Build());
                    }

                }).Start();
            }
        }

        protected override void ProtectedFlashNotificationAsync(string message, bool flashLaterIfNotVisible, TimeSpan duration, Action callback)
        {
            new Thread(() =>
            {
                RunActionUsingMainActivityAsync(mainActivity =>
                {
                    mainActivity.RunOnUiThread(() =>
                    {
                        int shortToasts = (int)Math.Ceiling(duration.TotalSeconds / 2);  // each short toast is 2 seconds.

                        for (int i = 0; i < shortToasts; ++i)
                            Toast.MakeText(mainActivity, message, ToastLength.Short).Show();

                        if (callback != null)
                            callback();
                    });

                }, false, flashLaterIfNotVisible);

            }).Start();
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

        /// <summary>
        /// Enables the Bluetooth adapter, or prompts the user to do so if we cannot do this programmatically. Must not be called from the UI thread.
        /// </summary>
        /// <returns><c>true</c>, if Bluetooth was enabled, <c>false</c> otherwise.</returns>
        /// <param name="lowEnergy">If set to <c>true</c> low energy.</param>
        /// <param name="rationale">Rationale.</param>
        public override bool EnableBluetooth(bool lowEnergy, string rationale)
        {
            base.EnableBluetooth(lowEnergy, rationale);

            bool enabled = false;

            // ensure that the device has the required feature
            if (!_service.PackageManager.HasSystemFeature(lowEnergy ? PackageManager.FeatureBluetoothLe : PackageManager.FeatureBluetooth))
            {
                FlashNotificationAsync("This device does not have the Bluetooth " + (lowEnergy ? "Low Energy" : "") + " feature.", true);
                return enabled;
            }

            ManualResetEvent enableWait = new ManualResetEvent(false);

            // check whether bluetooth is enabled
            BluetoothManager bluetoothManager = _service.GetSystemService(Context.BluetoothService) as BluetoothManager;
            BluetoothAdapter bluetoothAdapter = bluetoothManager.Adapter;
            if (bluetoothAdapter == null || !bluetoothAdapter.IsEnabled)
            {
                // if it's not and if the user has previously denied bluetooth, quit now.
                if (_userDeniedBluetoothEnable)
                    enableWait.Set();
                else
                {
                    // bring up sensus
                    RunActionUsingMainActivityAsync(mainActivity =>
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
                                        _userDeniedBluetoothEnable = true;
                                    else if (resultIntent.Item1 == Result.Ok)
                                        enabled = true;

                                    enableWait.Set();
                                });

                            }
                            catch (Exception ex)
                            {
                                Logger.Log("Failed to start Bluetooth:  " + ex.Message, LoggingLevel.Normal, GetType());
                                enableWait.Set();
                            }
                        });

                    }, true, false);
                }
            }
            else
            {
                enabled = true;
                enableWait.Set();
            }

            enableWait.WaitOne();

            if (enabled)
                _userDeniedBluetoothEnable = false;

            return enabled;
        }

        #endregion

        #region callback scheduling

        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            DateTime callbackTime = DateTime.Now.AddMilliseconds(initialDelayMS);
            Logger.Log("Callback " + callbackId + " scheduled for " + callbackTime + " (repeating).", LoggingLevel.Normal, GetType());
            ScheduleCallbackAlarm(CreateCallbackPendingIntent(callbackId, true, repeatDelayMS, repeatLag), callbackId, callbackTime);
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS)
        {
            DateTime callbackTime = DateTime.Now.AddMilliseconds(delayMS);
            Logger.Log("Callback " + callbackId + " scheduled for " + callbackTime + " (one-time).", LoggingLevel.Normal, GetType());
            ScheduleCallbackAlarm(CreateCallbackPendingIntent(callbackId, false, 0, false), callbackId, callbackTime);
        }

        private PendingIntent CreateCallbackPendingIntent(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag)
        {
            Intent callbackIntent = new Intent(_service, typeof(AndroidSensusService));
            callbackIntent.PutExtra(SENSUS_CALLBACK_KEY, true);
            callbackIntent.PutExtra(SENSUS_CALLBACK_ID_KEY, callbackId);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEATING_KEY, repeating);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEAT_DELAY_KEY, repeatDelayMS);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEAT_LAG_KEY, repeatLag);

            return CreateCallbackPendingIntent(callbackIntent);
        }

        public PendingIntent CreateCallbackPendingIntent(Intent callbackIntent)
        {
            // upon hash collisions for the request code, the previous intent will simply be canceled.
            return PendingIntent.GetService(_service, callbackIntent.GetStringExtra(SENSUS_CALLBACK_ID_KEY).GetHashCode(), callbackIntent, PendingIntentFlags.CancelCurrent);
        }

        public void ScheduleCallbackAlarm(PendingIntent callbackPendingIntent, string callbackId, DateTime callbackTime)
        {
            lock (_callbackIdPendingIntent)
            {
                // update pending intent associated with the callback id. we'll need the updated pending intent if/when
                // we which to unschedule the alarm.
                _callbackIdPendingIntent[callbackId] = callbackPendingIntent;

                AlarmManager alarmManager = _service.GetSystemService(Context.AlarmService) as AlarmManager;

                long delayMS = (long)(callbackTime - DateTime.Now).TotalMilliseconds;
                long callbackTimeMS = Java.Lang.JavaSystem.CurrentTimeMillis() + delayMS;

                // https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
#if __ANDROID_23__
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API level 23 added "while idle" option, making things even tighter.
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                    alarmManager.SetExact(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API level 19 differentiated Set (loose) from SetExact (tight)
                else
#endif
                {
                    alarmManager.Set(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API 1-18 treats Set as a tight alarm
                }
            }
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            lock (_callbackIdPendingIntent)
            {
                PendingIntent callbackPendingIntent;
                if (_callbackIdPendingIntent.TryGetValue(callbackId, out callbackPendingIntent))
                {
                    AlarmManager alarmManager = _service.GetSystemService(Context.AlarmService) as AlarmManager;
                    alarmManager.Cancel(callbackPendingIntent);
                    _callbackIdPendingIntent.Remove(callbackId);
                }
            }
        }

        #endregion

        #region device awake / sleep

        public override void KeepDeviceAwake()
        {
            lock (_wakeLock)
            {
                _wakeLock.Acquire();
                Logger.Log("Wake lock acquisition count:  " + ++_wakeLockAcquisitionCount, LoggingLevel.Verbose, GetType());
            }
        }

        public override void LetDeviceSleep()
        {
            lock (_wakeLock)
            {
                _wakeLock.Release();
                Logger.Log("Wake lock acquisition count:  " + --_wakeLockAcquisitionCount, LoggingLevel.Verbose, GetType());
            }
        }

        public override void BringToForeground()
        {
            ManualResetEvent foregroundWait = new ManualResetEvent(false);

            RunActionUsingMainActivityAsync(mainActivity =>
                {
                    foregroundWait.Set();

                }, true, false);

            foregroundWait.WaitOne();
        }

        #endregion
    }
}