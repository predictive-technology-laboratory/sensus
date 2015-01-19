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
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.Probes.User
{
    public class Prompt
    {
        /// <summary>
        /// Lock object forcing execution of one prompt at a time.
        /// </summary>
        private static readonly object _staticLockObject = new object();

        private PromptOutputType _outputType;
        private string _outputMessage;
        private PromptInputType _inputType;
        private ScriptDatum _inputDatum;
        private ManualResetEvent _inputDatumWait;
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
            _inputDatumWait = new ManualResetEvent(false);
            _requireNonEmptyInput = false;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType)
            : this()
        {
            _outputType = outputType;
            _outputMessage = outputMessage;
            _inputType = inputType;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, bool requireNonEmptyInput, int numTries)
            : this(outputType, outputMessage, inputType)
        {
            _requireNonEmptyInput = requireNonEmptyInput;
            _numTries = numTries;

            if (_requireNonEmptyInput)
                _outputMessage += " (response required)";
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, Prompt nextPrompt, string nextPromptValue)
            : this(outputType, outputMessage, inputType)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Prompt(PromptOutputType outputType, string outputMessage, PromptInputType inputType, bool requireNonEmptyInput, int numTries, Prompt nextPrompt, string nextPromptValue)
            : this(outputType, outputMessage, inputType, requireNonEmptyInput, numTries)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Task<ScriptDatum> RunAsync()
        {
            return Task.Run<ScriptDatum>(() =>
                {
                    lock (_staticLockObject)
                    {
                        _inputDatumWait.Reset();
                        RunAsyncPrivate();
                        _inputDatumWait.WaitOne();

                        return _inputDatum;
                    }
                });
        }

        private async void RunAsyncPrivate()
        {
            _inputDatum = null;

            string inputText = null;
            int triesLeft = _numTries;
            do
            {
                if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Text)
                    inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, false);
                else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.Voice)
                    inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, true);
                else if (_outputType == PromptOutputType.Text && _inputType == PromptInputType.None)
                    SensusServiceHelper.Get().FlashNotification(_outputMessage);
                else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Text)
                {
                    SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage, true);
                    inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, false);
                }
                else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.Voice)
                {
                    SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage, true);
                    inputText = await SensusServiceHelper.Get().PromptForInputAsync(_outputMessage, true);
                }
                else if (_outputType == PromptOutputType.Voice && _inputType == PromptInputType.None)
                    SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage, false);
                else
                    SensusServiceHelper.Get().Logger.Log("Prompt failure:  Unrecognized output/input setup:  " + _outputType + " -> " + _inputType, LoggingLevel.Normal);
            }
            while (inputText == null && _requireNonEmptyInput && --triesLeft > 0);

            if (_inputType != PromptInputType.None)
                _inputDatum = new ScriptDatum(null, DateTimeOffset.UtcNow, inputText);

            _inputDatumWait.Set();
        }
    }
}
