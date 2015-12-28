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
using SensusService.Exceptions;

namespace SensusUI.Inputs
{
    public class VoiceInput : Input
    {
        private string _outputMessage;
        private string _outputMessageRerun;
        private string _response;
        private bool _enabled;

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

        public override object Value
        {
            get
            {
                return _response;
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
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
            Construct("", "");
        }

        public VoiceInput(string outputMessage, string outputMessageRerun)
            : base()
        {
            Construct(outputMessage, outputMessageRerun);
        }

        public VoiceInput(string name, string outputMessage, string outputMessageRerun)
            : base(name, null)
        {
            Construct(outputMessage, outputMessageRerun);
        }

        private void Construct(string outputMessage, string outputMessageRerun)
        {
            _enabled = true;
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
        }

        public override View GetView(int index)
        {
            return null;
        }

        protected override void SetView(View value)
        {
            new SensusException("Cannot set View on VoiceInput.");
        }

        public void RunAsync(bool isRerun, DateTimeOffset firstRunTimestamp, Action<string> callback)
        {
            new Thread(() =>
                {                    
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
                                    Viewed = true;

                                    if (string.IsNullOrWhiteSpace(response))
                                        response = null;

                                    _response = response;

                                    Complete = _response != null;

                                    callback(_response);
                                });
                        });

                }).Start();
        }
    }
}