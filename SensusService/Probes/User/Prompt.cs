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

using System;
using System.Threading.Tasks;

namespace SensusService.Probes.User
{
    public class Prompt
    {
        private static readonly object _staticLockObject = new object();
        private static bool _promptIsRunning = false;

        private PromptOutputType _outputType;
        private string _outputMessage;
        private string _outputMessageRerun;
        private PromptInputType _inputType;
        private ScriptDatum _inputDatum;
        private bool _requireNonEmptyInput;
        private int _numTries;
        private Prompt _nextPrompt;
        private string _nextPromptValue;

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

        public bool RequireNonEmptyInput
        {
            get { return _requireNonEmptyInput; }
            set { _requireNonEmptyInput = value; }
        }

        public int NumTries
        {
            get { return _numTries; }
            set { _numTries = value; }
        }

        public Prompt NextPrompt
        {
            get { return _nextPrompt; }
            set { _nextPrompt = value; }
        }

        public string NextPromptValue
        {
            get { return _nextPromptValue; }
            set { _nextPromptValue = value; }
        }

        /// <summary>
        /// Constructor for JSON deserialization.
        /// </summary>
        private Prompt()
        {
            _requireNonEmptyInput = false;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType)
            : this()
        {
            _outputType = outputType;
            _outputMessage = outputMessage;
            _outputMessageRerun = outputMessageRerun;
            _inputType = inputType;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType, bool requireNonEmptyInput, int numTries)
            : this(outputType, outputMessage, outputMessageRerun, inputType)
        {
            _requireNonEmptyInput = requireNonEmptyInput;
            _numTries = numTries;

            if (_requireNonEmptyInput)
                _outputMessage += " (response required)";
        }

        public Prompt(PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType, Prompt nextPrompt, string nextPromptValue)
            : this(outputType, outputMessage, outputMessageRerun, inputType)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, string outputMessageRerun, PromptInputType inputType, bool requireNonEmptyInput, int numTries, Prompt nextPrompt, string nextPromptValue)
            : this(outputType, outputMessage, outputMessageRerun, inputType, requireNonEmptyInput, numTries)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Task<ScriptDatum> RunAsync(Datum previous, Datum current, bool isRerun, DateTimeOffset firstRunTimestamp)
        {
            return Task.Run<ScriptDatum>(async () =>
                {
                    lock (_staticLockObject)
                    {
                        if (_inputDatum != null)
                            return _inputDatum;

                        if (_promptIsRunning)
                            return null;
                        else
                            _promptIsRunning = true;
                    }

                    string message = _outputMessage;
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

                    string inputText = null;
                    int triesLeft = _numTries;
                    do
                    {
                        if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Text)
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(message, false);
                        else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Voice)
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(message, true);
                        else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.None)
                            await SensusServiceHelper.Get().FlashNotificationAsync(message);
                        else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Text)
                        {
                            await SensusServiceHelper.Get().TextToSpeechAsync(message);
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(message, false);
                        }
                        else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Voice)
                        {
                            await SensusServiceHelper.Get().TextToSpeechAsync(message);
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(message, true);
                        }
                        else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.None)
                            await SensusServiceHelper.Get().TextToSpeechAsync(message);
                        else
                            SensusServiceHelper.Get().Logger.Log("Prompt failure:  Unrecognized output/input setup:  " + _outputType + " -> " + _inputType, LoggingLevel.Normal);

                        if (string.IsNullOrWhiteSpace(inputText))
                            inputText = null;
                    }
                    while (inputText == null && _requireNonEmptyInput && --triesLeft > 0);

                    if (inputText != null)
                        _inputDatum = new ScriptDatum(null, DateTimeOffset.UtcNow, inputText, current == null ? null : current.Id);

                    lock (_staticLockObject)
                        _promptIsRunning = false;

                    return _inputDatum;
                });
        }
    }
}
