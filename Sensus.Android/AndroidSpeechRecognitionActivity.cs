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
using Android.OS;
using Android.Speech;
using Android.Views;
using System;
using Xamarin.Forms;

namespace Sensus.Android
{
    [Activity(MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class AndroidSpeechRecognitionActivity : Activity, IRecognitionListener
    {
        private SpeechRecognizer _recognizer;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetTheme(Resource.Style.Theme_Transparent);

            _recognizer = SpeechRecognizer.CreateSpeechRecognizer(this);
            _recognizer.SetRecognitionListener(this);

            Window.AddFlags(global::Android.Views.WindowManagerFlags.DismissKeyguard);
            Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked);
            Window.AddFlags(global::Android.Views.WindowManagerFlags.TurnScreenOn);

            Forms.Init(this, bundle);
        }

        protected override void OnResume()
        {
            base.OnResume();

            Intent intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
            intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
            intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 15000);
            intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            intent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);

            string prompt = Intent.GetStringExtra(RecognizerIntent.ExtraPrompt);
            if (prompt != null)
                intent.PutExtra(RecognizerIntent.ExtraPrompt, prompt);

            _recognizer.StartListening(intent);
        }

        public void OnBeginningOfSpeech()
        {
            int x = 1;
        }

        public void OnBufferReceived(byte[] buffer)
        {
            int x = 1;
        }

        public void OnEndOfSpeech()
        {
            int x = 1;
        }

        public void OnError(SpeechRecognizerError error)
        {
            int x = 1;
        }

        public void OnEvent(int eventType, Bundle @params)
        {
            int x = 1;
        }

        public void OnPartialResults(Bundle partialResults)
        {
            int x = 1;
        }

        public void OnReadyForSpeech(Bundle @params)
        {
            int x = 1;
        }

        public void OnResults(Bundle results)
        {
            Intent result = new Intent();
            result.PutStringArrayListExtra(SpeechRecognizer.ResultsRecognition, results.GetStringArrayList(SpeechRecognizer.ResultsRecognition));
            SetResult(Result.Ok, result);
            Finish();
        }

        public void OnRmsChanged(float rmsdB)
        {
            int x = 1;
        }

        protected override void OnPause()
        {
            base.OnPause();

            _recognizer.StopListening();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            try { _recognizer.Destroy(); }
            catch (Exception) { }
        }
    }
}