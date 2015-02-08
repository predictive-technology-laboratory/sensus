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

using Newtonsoft.Json;
using System;
using System.Threading;

namespace SensusService.Probes.User
{
    public class Prompt
    {
        #region static members
        private static readonly object _staticLockObject = new object();
        private static bool _promptIsRunning = false;
        #endregion

        private PromptOutputType _outputType;
        private string _outputMessage;
        private string _outputMessageRerun;
        private PromptInputType _inputType;
        private ScriptDatum _inputDatum;
        private bool _hasRun;

        public PromptOutputType OutputType
        {
            get { return _outputType; }
            set { _outputType = value; }
        }

        public string OutputMessage
        {
            get { return _outputMessage; }
            set { _outputMessage = value; }
        }

        public string OutputMessageRerun
        {
            get { return _outputMessageRerun; }
            set { _outputMessageRerun = value; }
        }

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

        public Prompt(PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType)
            : this()
        {
            _outputType = outputType;
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
            _inputType = inputType;
        }

        public void RunAsync(Datum previous, Datum current, bool isRerun, DateTimeOffset firstRunTimestamp, Action<ScriptDatum> callback)
        {
            new Thread(() =>
                {
                    lock (_staticLockObject)
                    {
                        if (_inputDatum != null)
                        {
                            callback(_inputDatum);
                            return;
                        }

                        if (_promptIsRunning)
                        {
                            callback(null);
                            return;
                        }
                        else
                            _promptIsRunning = true;
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
                                _inputDatum = new ScriptDatum(null, DateTimeOffset.UtcNow, inputText, current == null ? null : current.Id);

                            callback(_inputDatum);

                            _promptIsRunning = false;
                        });

                    if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Text)
                        SensusServiceHelper.Get().PromptForInputAsync(message, false, inputCallback);
                    else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Voice)
                        SensusServiceHelper.Get().PromptForInputAsync(message, true, inputCallback);
                    else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.None)
                    {
                        SensusServiceHelper.Get().FlashNotificationAsync(message);
                        _promptIsRunning = false;
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Text)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(message);
                        SensusServiceHelper.Get().PromptForInputAsync(message, false, inputCallback);
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Voice)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(message);
                        SensusServiceHelper.Get().PromptForInputAsync(message, true, inputCallback);
                    }
                    else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.None)
                    {
                        SensusServiceHelper.Get().TextToSpeechAsync(message);
                        _promptIsRunning = false;
                    }
                    else
                        SensusServiceHelper.Get().Logger.Log("Prompt failure:  Unrecognized output/input setup:  " + _outputType + " -> " + _inputType, LoggingLevel.Normal);
                    
                    _hasRun = true;

                }).Start();
        }
    }
}
