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

namespace SensusService.Probes.User
{
    public class Prompt
    {
        #region static members
        private static readonly object LOCKER = new object();
        private static bool PROMPT_IS_RUNNING = false;
        #endregion

        private string _name;
        private PromptOutputType _outputType;
        private string _outputMessage;
        private string _outputMessageRerun;
        private PromptInputType _inputType;
        private ScriptDatum _inputDatum;
        private bool _hasRun;

        [EntryStringUiProperty("Name:", true, 9)]
        public string Name
        {
            get{ return _name; }
            set{ _name = value; }
        }

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

        public ScriptDatum InputDatum
        {
            get { return _inputDatum; }
            set { _inputDatum = value; }
        }

        public bool HasRun
        {
            get { return _hasRun; }
            set { _hasRun = value; }
        }

        [JsonIgnore]
        public bool Complete
        {
            get { return _hasRun && (_inputType == PromptInputType.None || _inputDatum != null); }
        }

        /// <summary>
        /// Constructor for JSON deserialization.
        /// </summary>
        private Prompt()
        {
            _hasRun = false;
        }

        public Prompt(string name, PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType)
            : this()
        {
            _name = name;
            _outputType = outputType;
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
            _inputType = inputType;
        }

        public void RunAsync(Datum previous, Datum current, bool isRerun, DateTimeOffset firstRunTimestamp, Action<ScriptDatum> callback)
        {
            new Thread(() =>
                {
                    lock (LOCKER)
                    {
                        if (_inputDatum != null)
                        {
                            callback(_inputDatum);
                            return;
                        }

                        if (PROMPT_IS_RUNNING)
                        {
                            callback(null);
                            return;
                        }
                        else
                            PROMPT_IS_RUNNING = true;
                    }

                    string message = _outputMessage;

                    #region rerun processing
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

                        message = string.Format(_outputMessageRerun, daysAgoStr + " at " + firstRunTimestamp.ToLocalTime().DateTime.ToString("%h:%mm %tt"));
                    }
                    #endregion

                    Action<string> inputCallback = new Action<string>(inputText =>
                        {
                            if (string.IsNullOrWhiteSpace(inputText))
                                inputText = null;

                            if (inputText != null)
                                _inputDatum = new ScriptDatum(DateTimeOffset.UtcNow, inputText, current == null ? null : current.Id);

                            callback(_inputDatum);

                            PROMPT_IS_RUNNING = false;
                        });

                    if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Text)
                        SensusServiceHelper.Get().PromptForInputAsync(message, false, inputCallback);
                    else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Voice)
                        SensusServiceHelper.Get().PromptForInputAsync(message, true, inputCallback);
                    else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.None)
                    {
                        SensusServiceHelper.Get().FlashNotificationAsync(message, () =>
                            {
                                PROMPT_IS_RUNNING = false;
                            });
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Text)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(message, () =>
                            {
                                SensusServiceHelper.Get().PromptForInputAsync(message, false, inputCallback);
                            });
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Voice)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(message, () =>
                            {
                                SensusServiceHelper.Get().PromptForInputAsync(message, true, inputCallback);
                            });
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.None)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(message, () =>
                            {
                                PROMPT_IS_RUNNING = false;
                            });
                    }
                    else
                        SensusServiceHelper.Get().Logger.Log("Prompt failure:  Unrecognized output/input setup:  " + _outputType + " -> " + _inputType, LoggingLevel.Normal, GetType());
                    
                    _hasRun = true;

                }).Start();
        }

        public override string ToString()
        {
            return _name;
        }
    }
}