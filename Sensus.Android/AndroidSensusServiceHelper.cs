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
using Xamarin.Geolocation;
using SensusService.Probes.Location;
using SensusService.Probes;
using SensusService.Probes.Movement;

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        public const string MAIN_ACTIVITY_WILL_BE_SET = "MAIN-ACTIVITY-WILL-BE-SET";
        private const string SERVICE_NOTIFICATION_TAG = "SENSUS-SERVICE-NOTIFICATION";

        private AndroidSensusService _service;
        private ConnectivityManager _connectivityManager;
        private NotificationManager _notificationManager;
        private string _deviceId;
        private AndroidMainActivity _mainActivity;
        private ManualResetEvent _mainActivityWait;
        private readonly object _getMainActivityLocker = new object();
        private AndroidTextToSpeech _textToSpeech;
        private PowerManager.WakeLock _wakeLock;
        private bool _mainActivityWillBeSet;

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
            get { return _connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).IsConnected; }
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

        protected override Geolocator Geolocator
        {
            get
            {
                return new Geolocator(Application.Context);
            }
        }

        public bool MainActivityWillBeSet
        {
            get
            {
                return _mainActivityWillBeSet;
            }
            set
            {
                _mainActivityWillBeSet = value;
            }
        }

        public AndroidSensusServiceHelper()
        {
            _mainActivityWait = new ManualResetEvent(false);   
        }          

        public void SetService(AndroidSensusService service)
        {
            _service = service;
            _connectivityManager = _service.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            _notificationManager = _service.GetSystemService(Context.NotificationService) as NotificationManager;
            _deviceId = Settings.Secure.GetString(_service.ContentResolver, Settings.Secure.AndroidId);
            _textToSpeech = new AndroidTextToSpeech(_service);
            _wakeLock = (_service.GetSystemService(Context.PowerService) as PowerManager).NewWakeLock(WakeLockFlags.Partial, "SENSUS");           
        }

        public void GetMainActivityAsync(bool foreground, Action<AndroidMainActivity> callback)
        {
            // this must be done asynchronously because it blocks waiting for the activity to start. calling this method from the UI would create deadlocks.
            new Thread(() =>
                {
                    lock (_getMainActivityLocker)
                    {
                        if (_mainActivity == null || (foreground && !_mainActivity.IsForegrounded))
                        {                            
                            Logger.Log("Main activity is not started or is not in the foreground.", LoggingLevel.Normal, GetType());

                            if(_mainActivityWillBeSet)
                                Logger.Log("Main activity is about to be set. Will wait for it.", LoggingLevel.Normal, GetType());
                            else
                            {
                                Logger.Log("Starting main activity.", LoggingLevel.Normal, GetType());

                                _mainActivityWait.Reset();

                                // start the activity and wait for it to bind itself to the service
                                Intent intent = new Intent(_service, typeof(AndroidMainActivity));
                                intent.AddFlags(ActivityFlags.FromBackground | ActivityFlags.NewTask);
                                _service.StartActivity(intent);
                            }

                            // wait for main activity to be set
                            _mainActivityWait.WaitOne();

                            // wait for the UI to come up -- we don't want it to come up later and hide anything
                            _mainActivity.UiReadyWait.WaitOne();
                            Logger.Log("UI is ready.", LoggingLevel.Normal, GetType());
                        }

                        callback(_mainActivity);
                    }
                }).Start();
        }

        public void SetMainActivity(AndroidMainActivity value)
        {
            _mainActivity = value;

            if (_mainActivity == null)
            {
                _mainActivityWait.Reset();
                Logger.Log("Main activity has been unset.", LoggingLevel.Normal, GetType());
            }
            else
            {
                _mainActivityWait.Set();
                Logger.Log("Main activity has been set.", LoggingLevel.Normal, GetType());
            }
        }

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

                        GetMainActivityAsync(true, mainActivity =>
                            {
                                mainActivity.GetActivityResultAsync(intent, AndroidActivityResultRequestCode.PromptForFile, result =>
                                    {
                                        if (result != null && result.Item1 == Result.Ok)
                                            try
                                            {
                                                using (StreamReader file = new StreamReader(_service.ContentResolver.OpenInputStream(result.Item2.Data)))
                                                {
                                                    string content = file.ReadToEnd();
                                                    file.Close();
                                                    callback(content);
                                                }
                                            }
                                            catch (Exception ex) { Toast.MakeText(_service, "Error reading text file:  " + ex.Message, ToastLength.Long); }
                                    });
                            });
                    }
                    catch (ActivityNotFoundException) { Toast.MakeText(_service, "Please install a file manager from the Apps store.", ToastLength.Long); }
                    catch (Exception ex) { Toast.MakeText(_service, "Something went wrong while prompting you for a file to read:  " + ex.Message, ToastLength.Long); }

                }).Start();
        }

        public override void ShareFileAsync(string path, string subject)
        {
            new Thread(() =>
                {
                    try
                    {
                        Intent intent = new Intent(Intent.ActionSend);
                        intent.SetType("text/plain");
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                        if (!string.IsNullOrWhiteSpace(subject))
                            intent.PutExtra(Intent.ExtraSubject, subject);

                        Java.IO.File file = new Java.IO.File(path);
                        global::Android.Net.Uri uri = FileProvider.GetUriForFile(_service, "edu.virginia.sie.ptl.sensus.fileprovider", file);
                        intent.PutExtra(Intent.ExtraStream, uri);

                        // run from main activity to get a smoother transition back to sensus
                        GetMainActivityAsync(true, mainActivity => mainActivity.StartActivity(intent));
                    }
                    catch (Exception ex) { Logger.Log("Failed to start intent to share file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal, GetType()); }

                }).Start();
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            _textToSpeech.SpeakAsync(text, callback);
        }

        public override void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback)
        {
            new Thread(() =>
                {
                    string input = null;
                    ManualResetEvent dialogDismissWait = new ManualResetEvent(false);

                    GetMainActivityAsync(true, mainActivity => mainActivity.RunOnUiThread(() =>
                        {
                            EditText inputEdit = new EditText(mainActivity);

                            AlertDialog dialog = new AlertDialog.Builder(mainActivity)
                                                 .SetTitle("Sensus is requesting input...")
                                                 .SetMessage(prompt)
                                                 .SetView(inputEdit)
                                                 .SetPositiveButton("OK", (o, e) =>
                                                     {
                                                         input = inputEdit.Text;
                                                     })
                                                 .SetNegativeButton("Cancel", (o, e) => { })
                                                 .Create();

                            // SetOnDismissListener was added to the AlertDialog.Builder class at API level 17. Call it here to keep us at API level 16.
                            dialog.SetOnDismissListener(new AndroidOnDismissListener(() =>
                                {
                                    dialogDismissWait.Set();
                                }));

                            ManualResetEvent dialogShowWait = new ManualResetEvent(false);

                            dialog.SetOnShowListener(new AndroidOnShowListener(() =>
                                {
                                    dialogShowWait.Set();
                                }));

                            // dismiss the keyguard when dialog appears
                            dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.DismissKeyguard);
                            dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked);
                            dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.TurnScreenOn);
                            dialog.Window.SetSoftInputMode(global::Android.Views.SoftInput.StateAlwaysVisible);

                            // dim whatever is behind the dialog
                            dialog.Window.AddFlags(global::Android.Views.WindowManagerFlags.DimBehind);
                            dialog.Window.Attributes.DimAmount = 0.75f;

                            dialog.Show();

                            #region voice recognizer
                            if (startVoiceRecognizer)
                            {
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
                            }
                            #endregion
                        }));

                    dialogDismissWait.WaitOne();
                    callback(input);

                }).Start();
        }

        #region notifications
        public override void UpdateApplicationStatus(string status)
        {
            IssueNotification("Sensus", status == null ? null : (status + " (tap to open)"), true, SERVICE_NOTIFICATION_TAG);
        }

        public override void IssueNotificationAsync(string message, string id)
        {
            IssueNotification("Sensus", message, false, id);
        }

        private void IssueNotification(string title, string body, bool sticky, string tag)
        {            
            if (body == null)
                _notificationManager.Cancel(tag, 0);
            else
            {
                TaskStackBuilder stackBuilder = TaskStackBuilder.Create(_service);
                stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(AndroidMainActivity)));
                stackBuilder.AddNextIntent(new Intent(_service, typeof(AndroidMainActivity)));
                PendingIntent pendingIntent = stackBuilder.GetPendingIntent(0, PendingIntentFlags.UpdateCurrent);

                Notification notification = new Notification.Builder(_service)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetSmallIcon(Resource.Drawable.ic_launcher)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(!sticky)
                    .SetOngoing(sticky).Build();

                _notificationManager.Notify(tag, 0, notification);
            }
        }

        public override void FlashNotificationAsync(string message, Action callback)
        {
            new Thread(() =>
                {
                    GetMainActivityAsync(false, mainActivity =>
                        {
                            mainActivity.RunOnUiThread(() =>
                                {
                                    Toast.MakeText(mainActivity, message, ToastLength.Long).Show();

                                    if(callback != null)
                                        callback();
                                });
                        });
                }).Start();
        } 
        #endregion

        #region callback scheduling
        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, string userNotificationMessage)
        {            
            long initialTimeMS = Java.Lang.JavaSystem.CurrentTimeMillis() + initialDelayMS;

            Logger.Log("Callback " + callbackId + " scheduled for " + (new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(initialTimeMS)) + " (repeating).", LoggingLevel.Debug, GetType());

            AlarmManager alarmManager = _service.GetSystemService(Context.AlarmService) as AlarmManager;
            alarmManager.SetRepeating(AlarmType.RtcWakeup, initialTimeMS, repeatDelayMS, GetCallbackIntent(callbackId, true));
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS, string userNotificationMessage)
        {
            long timeMS = Java.Lang.JavaSystem.CurrentTimeMillis() + delayMS;

            Logger.Log("Callback " + callbackId + " scheduled for " + (new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, new TimeSpan()).AddMilliseconds(timeMS)) + " (one-time).", LoggingLevel.Debug, GetType());

            AlarmManager alarmManager = _service.GetSystemService(Context.AlarmService) as AlarmManager;
            alarmManager.Set(AlarmType.RtcWakeup, timeMS, GetCallbackIntent(callbackId, false));
        }

        protected override void UnscheduleCallback(string callbackId, bool repeating)
        {
            AlarmManager alarmManager = _service.GetSystemService(Context.AlarmService) as AlarmManager;
            alarmManager.Cancel(GetCallbackIntent(callbackId, repeating));
        }

        private PendingIntent GetCallbackIntent(string callbackId, bool repeating)
        {
            Intent serviceIntent = new Intent(_service, typeof(AndroidSensusService));
            serviceIntent.PutExtra(SENSUS_CALLBACK_KEY, true);
            serviceIntent.PutExtra(SENSUS_CALLBACK_ID_KEY, callbackId);
            serviceIntent.PutExtra(SENSUS_CALLBACK_REPEATING_KEY, repeating);

            return PendingIntent.GetService(_service, callbackId.GetHashCode(), serviceIntent, PendingIntentFlags.CancelCurrent);  // upon hash collisions, the previous intent will simply be canceled.
        }
        #endregion

        #region device awake / sleep
        public override void KeepDeviceAwake()
        {
            lock (_wakeLock)
                _wakeLock.Acquire();
        }

        public override void LetDeviceSleep()
        {
            lock (_wakeLock)
                _wakeLock.Release();
        }            
        #endregion

        public override void Stop()
        {
            base.Stop();

            UpdateApplicationStatus(null);

            if (_mainActivity != null)
                _mainActivity.Finish();

            if (_service != null)
                _service.StopSelf();
        }

        public override void Destroy()
        {
            UpdateApplicationStatus(null);

            _textToSpeech.Dispose();

            base.Destroy();
        }
    }
}