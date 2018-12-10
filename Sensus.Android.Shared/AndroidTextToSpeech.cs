//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Android.Speech.Tts;
using System;
using System.Collections.Generic;
using System.Threading;
using Android.OS;
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

        private readonly object _locker = new object();

        public AndroidTextToSpeech(AndroidSensusService service)
        {
            // initialize wait handles before passing the current object as a listener
            // below. if the listener OnInit method is called before the wait handles are
            // initialized we could get an NRE:  https://insights.xamarin.com/app/Sensus-Production/issues/1099
            _initWait = new ManualResetEvent(false);
            _utteranceWait = new ManualResetEvent(false);
            _disposed = false;

            // initialize speech module
            _textToSpeech = new TextToSpeech(service, this);
            _textToSpeech.SetLanguage(Java.Util.Locale.Default);
            _textToSpeech.SetOnUtteranceProgressListener(this);
        }

        public void OnInit(OperationResult status)
        {
            _initWait.Set();
        }

        public Task SpeakAsync(string text)
        {
            return Task.Run(() =>
            {
                lock (_locker)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _initWait.WaitOne();
                    _utteranceWait.Reset();
                    _utteranceIdToWaitFor = Guid.NewGuid().ToString();

                    // see the Backwards Compatibility article for more information
#if __ANDROID_21__
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    {
                        _textToSpeech.Speak(text, QueueMode.Add, null, _utteranceIdToWaitFor);
                    }
                    else
#endif
                    {
                        // ignore deprecation warning
#pragma warning disable 618
                        Dictionary<string, string> speakParams = new Dictionary<string, string>();
                        speakParams.Add(TextToSpeech.Engine.KeyParamUtteranceId, _utteranceIdToWaitFor);
                        _textToSpeech.Speak(text, QueueMode.Add, speakParams);
#pragma warning restore 618
                    }

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
            lock (_locker)
            {
                base.Dispose(disposing);

                _disposed = true;

                try { _textToSpeech.Shutdown(); }
                catch (Exception) { }
            }
        }
    }
}
