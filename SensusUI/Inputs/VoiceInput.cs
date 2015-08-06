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

using Newtonsoft.Json;
using System;
using System.Threading;
using SensusUI.UiProperties;
using SensusService.Probes.User;
using SensusService;
using Xamarin.Forms;

namespace SensusUI.Inputs
{
    public class VoiceInput : Input
    {
        #region static members

        private static readonly object LOCKER = new object();
        private static bool IS_RUNNING = false;

        #endregion

        private string _outputMessage;
        private string _outputMessageRerun;
        private string _response;
        private bool _hasRun;

        [EntryStringUiProperty("Output Message:", true, 11)]
        public string OutputMessage
        {
            get { return _outputMessage; }
            set { _outputMessage = value; }
        }

        [EntryStringUiProperty("Output Message Rerun:", true, 12)]
        public string OutputMessageRerun
        {
            get { return _outputMessageRerun; }
            set { _outputMessageRerun = value; }
        }

        public string Response
        {
            get { return _response; }
            set { _response = value; }
        }

        public bool HasRun
        {
            get { return _hasRun; }
            set { _hasRun = value; }
        }

        public override bool Complete
        {
            get { return _hasRun && _response != null; }
        }

        public override string DefaultName
        {
            get
            {
                return "Voice Prompt";
            }
        }

        public VoiceInput()
        {
            _outputMessage = "";
            _outputMessageRerun = "";
        }

        public VoiceInput(string outputMessage, string outputMessageRerun)
            : base()
        {
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
        }

        public VoiceInput(string name, string outputMessage, string outputMessageRerun)
            : base(name, null)
        {
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
        }

        public void RunAsync(Datum triggeringDatum, bool isRerun, DateTimeOffset firstRunTimestamp, Action<string> callback)
        {
            new Thread(() =>
                {
                    // only one prompt can run at a time...enforce that here.
                    lock (LOCKER)
                    {
                        // calling after a previous call has completed returns the same response
                        if (_response != null)
                        {
                            callback(_response);
                            return;
                        }

                        // calling while a previous call is in progress returns null
                        if (IS_RUNNING)
                        {
                            callback(null);
                            return;
                        }
                        else
                            IS_RUNNING = true;
                    }

                    string outputMessage = _outputMessage;

                    #region temporal analysis for rerun
                    if (isRerun && !string.IsNullOrWhiteSpace(_outputMessageRerun))
                    {
                        TimeSpan promptAge = DateTimeOffset.UtcNow - firstRunTimestamp;

                        int daysAgo = (int)promptAge.TotalDays;
                        string daysAgoStr;
                        if (daysAgo == 0)
                            daysAgoStr = "today";
                        else if (daysAgo == 1)
                            daysAgoStr = "yesterday";
                        else
                            daysAgoStr = promptAge.TotalDays + " days ago";

                        outputMessage = string.Format(_outputMessageRerun, daysAgoStr + " at " + firstRunTimestamp.ToLocalTime().DateTime.ToString("h:mm tt"));
                    }
                    #endregion

                    SensusServiceHelper.Get().TextToSpeechAsync(outputMessage, () =>
                        {
                            SensusServiceHelper.Get().RunVoicePromptAsync(outputMessage, response =>
                                {
                                    if (string.IsNullOrWhiteSpace(response))
                                        response = null;

                                    _response = response;

                                    callback(_response);

                                    // allow other prompts to run
                                    IS_RUNNING = false;
                                });
                        });
                    
                    _hasRun = true;

                }).Start();
        }

        public override View CreateView(out Func<object> valueRetriever)
        {
            valueRetriever = null;
            return null;
        }
    }
}