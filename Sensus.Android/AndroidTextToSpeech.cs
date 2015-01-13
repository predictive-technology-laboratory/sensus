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
 
using Android.Speech.Tts;
using System;
using System.Threading;

namespace Sensus.Android
{
    public class AndroidTextToSpeech : Java.Lang.Object, TextToSpeech.IOnInitListener
    {
        private TextToSpeech _textToSpeech;
        private ManualResetEvent _textToSpeechInitWait;

        public AndroidTextToSpeech(AndroidSensusService service)
        {
            _textToSpeech = new TextToSpeech(service, this);
            _textToSpeechInitWait = new ManualResetEvent(false);
        }

        void TextToSpeech.IOnInitListener.OnInit(OperationResult status)
        {
            _textToSpeech.SetLanguage(Java.Util.Locale.Default);
            _textToSpeechInitWait.Set();
        }

        public void Speak(string text)
        {
            // wait for TTS to initialize
            _textToSpeechInitWait.WaitOne();
            _textToSpeech.Speak(text, QueueMode.Add, null);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try { _textToSpeech.Shutdown(); }
            catch (Exception) { }
        }
    }
}