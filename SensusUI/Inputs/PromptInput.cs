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
    public class PromptInput : Input
    {
        #region static members
        private static readonly object LOCKER = new object();
        private static bool PROMPT_IS_RUNNING = false;
        #endregion

        private PromptOutputType _outputType;
        private string _outputMessage;
        private string _outputMessageRerun;
        private PromptInputType _inputType;
        private string _response;
        private bool _hasRun;

        [ListUiProperty("Output Type:", true, 10, new object[] { PromptOutputType.Text, PromptOutputType.Voice })]
        public PromptOutputType OutputType
        {
            get { return _outputType; }
            set { _outputType = value; }
        }

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

        [ListUiProperty("Input Type", true, 13, new object [] { PromptInputType.None, PromptInputType.Text, PromptInputType.Voice })]
        public PromptInputType InputType
        {
            get { return _inputType; }
            set { _inputType = value; }
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
            get { return _hasRun && (_inputType == PromptInputType.None || _response != null); }
        }

        public override string DisplayName
        {
            get
            {
                return "Voice Prompt";
            }
        }

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        protected PromptInput()
        {
            _outputType = PromptOutputType.Text;
            _outputMessage = "";
            _outputMessageRerun = "";
            _inputType = PromptInputType.Text;
            _hasRun = false;
        }

        public PromptInput(string name, PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType)
            : base(name, null)
        {
            _outputType = outputType;
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
            _inputType = inputType;
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
                        if (PROMPT_IS_RUNNING)
                        {
                            callback(null);
                            return;
                        }
                        else
                            PROMPT_IS_RUNNING = true;
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

                    // action to execute when user has provided a response
                    Action<string> responseCallback = new Action<string>(response =>
                        {
                            // don't treat null/whitespace the same as no input
                            if (string.IsNullOrWhiteSpace(response))
                                response = null;

                            _response = response;

                            callback(_response);

                            // allow other prompts to run
                            PROMPT_IS_RUNNING = false;
                        });

                    if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Text)
                        SensusServiceHelper.Get().PromptForInputAsync(outputMessage, false, responseCallback);
                    else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Voice)
                        SensusServiceHelper.Get().PromptForInputAsync(outputMessage, true, responseCallback);
                    else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.None)
                    {
                        SensusServiceHelper.Get().FlashNotificationAsync(outputMessage, () =>
                            {
                                PROMPT_IS_RUNNING = false;
                            });
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Text)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(outputMessage, () =>
                            {
                                SensusServiceHelper.Get().PromptForInputAsync(outputMessage, false, responseCallback);
                            });
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Voice)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(outputMessage, () =>
                            {
                                SensusServiceHelper.Get().PromptForInputAsync(outputMessage, true, responseCallback);
                            });
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.None)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(outputMessage, () =>
                            {
                                PROMPT_IS_RUNNING = false;
                            });
                    }
                    else
                        SensusServiceHelper.Get().Logger.Log("Prompt failure:  Unrecognized output/input setup:  " + _outputType + " -> " + _inputType, LoggingLevel.Normal, GetType());
                    
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