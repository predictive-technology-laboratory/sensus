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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.Android
{
    public class AndroidTextToSpeech : UtteranceProgressListener, TextToSpeech.IOnInitListener
    {
        private TextToSpeech _textToSpeech;
        private ManualResetEvent _initWait;
        private string _utteranceIdToWaitFor;
        private ManualResetEvent _utteranceWait;
        private bool _disposed;

        public AndroidTextToSpeech(AndroidSensusService service)
        {
            _textToSpeech = new TextToSpeech(service, this);
            _initWait = new ManualResetEvent(false);
            _utteranceWait = new ManualResetEvent(false);
            _disposed = false;

            _textToSpeech.SetOnUtteranceProgressListener(this);
        }

        public void OnInit(OperationResult status)
        {
            _textToSpeech.SetLanguage(Java.Util.Locale.Default);
            _initWait.Set();
        }

        public Task SpeakAsync(string text)
        {
            return Task.Run(() =>
                {
                    lock (this)
                    {
                        if (_disposed)
                            return;

                        _initWait.WaitOne();

                        _utteranceIdToWaitFor = Guid.NewGuid().ToString();
                        Dictionary<string, string> speakParams = new Dictionary<string, string>();
                        speakParams.Add(TextToSpeech.Engine.KeyParamUtteranceId, _utteranceIdToWaitFor);

                        _utteranceWait.Reset();
                        _textToSpeech.Speak(text, QueueMode.Add, speakParams);
                        _utteranceWait.WaitOne();
                    }
                });
        }

        public override void OnStart(string utteranceId)
        {
        }

        public override void OnDone(string utteranceId)
        {
            if (utteranceId == _utteranceIdToWaitFor)
                _utteranceWait.Set();
        }

        public override void OnError(string utteranceId)
        {
            if (utteranceId == _utteranceIdToWaitFor)
                _utteranceWait.Set();
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                base.Dispose(disposing);

                _disposed = true;

                try { _textToSpeech.Shutdown(); }
                catch (Exception) { }
            }
        }
    }
}