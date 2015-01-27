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
        private PromptInputType _inputType;
        private int _timeoutMS;
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

        public PromptInputType InputType
        {
            get { return _inputType; }
            set { _inputType = value; }
        }

        public int TimeoutMS
        {
            get { return _timeoutMS; }
            set { _timeoutMS = value; }
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
            _timeoutMS = 30000;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, int timeoutMS)
            : this()
        {
            _outputType = outputType;
            _outputMessage = outputMessage;
            _inputType = inputType;
            _timeoutMS = timeoutMS;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, int timeoutMS, bool requireNonEmptyInput, int numTries)
            : this(outputType, outputMessage, inputType, timeoutMS)
        {
            _requireNonEmptyInput = requireNonEmptyInput;
            _numTries = numTries;

            if (_requireNonEmptyInput)
                _outputMessage += " (response required)";
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, int timeoutMS, Prompt nextPrompt, string nextPromptValue)
            : this(outputType, outputMessage, inputType, timeoutMS)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, int timeoutMS, bool requireNonEmptyInput, int numTries, Prompt nextPrompt, string nextPromptValue)
            : this(outputType, outputMessage, inputType, timeoutMS, requireNonEmptyInput, numTries)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Task<ScriptDatum> RunAsync(Datum previous, Datum current)
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

                    string inputText = null;
                    int triesLeft = _numTries;
                    do
                    {
                        if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Text)
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, false, _timeoutMS);
                        else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Voice)
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, true, _timeoutMS);
                        else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.None)
                            await SensusServiceHelper.Get().FlashNotificationAsync(_outputMessage);
                        else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Text)
                        {
                            await SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage);
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, false, _timeoutMS);
                        }
                        else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Voice)
                        {
                            await SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage);
                            inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, true, _timeoutMS);
                        }
                        else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.None)
                            await SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage);
                        else
                            SensusServiceHelper.Get().Logger.Log("Prompt failure:  Unrecognized output/input setup:  " + _outputType + " -> " + _inputType, LoggingLevel.Normal);

                        if (string.IsNullOrWhiteSpace(inputText))
                            inputText = null;
                    }
                    while (inputText == null && _requireNonEmptyInput && --triesLeft > 0);

                    if (inputText != null)
                        _inputDatum = new ScriptDatum(null, DateTimeOffset.UtcNow, inputText);

                    lock (_staticLockObject)
                        _promptIsRunning = false;

                    return _inputDatum;
                });
        }
    }
}
