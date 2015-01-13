#region copyright
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
#endregion
 
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Speech;
using Android.Support.V4.Content;
using Android.Widget;
using SensusService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin;
using Xamarin.Geolocation;

namespace Sensus.Android
{
    public class AndroidSensusServiceHelper : SensusServiceHelper
    {
        public const string INTENT_EXTRA_NAME_PING = "PING-SENSUS";

        private ConnectivityManager _connectivityManager;
        private readonly string _deviceId;
        private MainActivity _mainActivity;

        public MainActivity MainActivity
        {
            get { return _mainActivity; }
            set { _mainActivity = value; }
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
                BatteryStatus status = (BatteryStatus)Application.Context.RegisterReceiver(null, filter).GetIntExtra(BatteryManager.ExtraStatus, -1);
                return status == BatteryStatus.Charging || status == BatteryStatus.Full;
            }
        }

        public override string DeviceId
        {
            get { return _deviceId; }
        }

        public override bool DeviceHasMicrophone
        {
            get { return PackageManager.FeatureMicrophone == "android.hardware.microphone"; }
        }

        public AndroidSensusServiceHelper()
            : base(new Geolocator(Application.Context))
        {
            _connectivityManager = Application.Context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            _deviceId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
        }

        protected override void InitializeXamarinInsights()
        {
            Insights.Initialize(XAMARIN_INSIGHTS_APP_KEY, Application.Context);
        }

        public override Task<string> PromptForAndReadTextFile(string promptTitle)
        {
            return Task.Run<string>(async () =>
                {
                    Intent intent = new Intent(Intent.ActionGetContent);
                    intent.SetType("*/*");
                    intent.AddCategory(Intent.CategoryOpenable);

                    try
                    {
                        Tuple<Result, Intent> result = await (SensusServiceHelper.Get() as AndroidSensusServiceHelper).MainActivity.GetActivityResult(intent, ActivityResultRequestCode.PromptForFile);
                        if (result.Item1 == Result.Ok)
                            try
                            {
                                using (StreamReader file = new StreamReader(MainActivity.ContentResolver.OpenInputStream(result.Item2.Data)))
                                    return file.ReadToEnd();
                            }
                            catch (Exception ex) { Toast.MakeText(MainActivity, "Error reading text file:  " + ex.Message, ToastLength.Long); }
                    }
                    catch (ActivityNotFoundException) { Toast.MakeText(MainActivity, "Please install a file manager from the Apps store.", ToastLength.Long); }
                    catch (Exception ex) { Toast.MakeText(MainActivity, "Something went wrong while prompting you for a file to read:  " + ex.Message, ToastLength.Long); }

                    return null;
                });
        }

        public override void ShareFile(string path, string subject)
        {
            try
            {
                Intent intent = new Intent(Intent.ActionSend);
                intent.SetType("text/plain");
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                if (!string.IsNullOrWhiteSpace(subject))
                    intent.PutExtra(Intent.ExtraSubject, subject);

                Java.IO.File file = new Java.IO.File(path);
                global::Android.Net.Uri uri = FileProvider.GetUriForFile(Application.Context, "edu.virginia.sie.ptl.sensus.fileprovider", file);
                intent.PutExtra(Intent.ExtraStream, uri);

                // if we're outside the context of the main activity (e.g., within the service), start a new activity
                if (_mainActivity == null)
                {
                    intent.AddFlags(ActivityFlags.NewTask);
                    Application.Context.StartActivity(intent);
                }
                // otherwise, start the activity off the main activity -- smoother transition back to sensus after the file has been shared
                else
                    _mainActivity.StartActivity(intent);
            }
            catch (Exception ex) { Logger.Log("Failed to start intent to share file \"" + path + "\":  " + ex.Message, LoggingLevel.Normal); }
        }

        protected override void StartSensusPings(int ms)
        {
            SetSensusMonitoringAlarm(ms);
        }

        protected override void StopSensusPings()
        {
            SetSensusMonitoringAlarm(-1);
        }

        public override void TextToSpeech(string text)
        {
            MainActivity.TextToSpeech(text);
        }

        public override Task<string> RecognizeSpeech(string prompt)
        {
            return Task.Run<string>(async () =>
                {
                    Intent intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                    intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                    intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
                    intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
                    intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 15000);
                    intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                    intent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);

                    if (prompt != null)
                        intent.PutExtra(RecognizerIntent.ExtraPrompt, prompt);

                    Tuple<Result, Intent> result = await MainActivity.GetActivityResult(intent, ActivityResultRequestCode.RecognizeSpeech);
                    if (result.Item1 == Result.Ok)
                    {
                        IList<string> matches = result.Item2.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                        if (matches.Count == 0)
                            return null;
                        else
                            return matches[0];
                    }
                    else
                        return null;
                });
        }

        private void SetSensusMonitoringAlarm(int ms)
        {
            Context context = Application.Context;
            AlarmManager alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            Intent serviceIntent = new Intent(context, typeof(AndroidSensusService));
            serviceIntent.PutExtra(INTENT_EXTRA_NAME_PING, true);
            PendingIntent pendingServiceIntent = PendingIntent.GetService(context, 0, serviceIntent, PendingIntentFlags.UpdateCurrent);

            if (ms > 0)
                alarmManager.SetRepeating(AlarmType.RtcWakeup, Java.Lang.JavaSystem.CurrentTimeMillis() + ms, ms, pendingServiceIntent);
            else
                alarmManager.Cancel(pendingServiceIntent);
        }
    }
}