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
        private PromptType _type;
        private string _message;
        private PromptResponseType _responseType;
        private Prompt _nextPrompt;
        private string _nextPromptValue;
        private bool _requireNonEmptyResponse;
        private int _numTries;

        public PromptType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public PromptResponseType ResponseType
        {
            get { return _responseType; }
            set { _responseType = value; }
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

        public bool RequireNonEmptyResponse
        {
            get { return _requireNonEmptyResponse; }
            set { _requireNonEmptyResponse = value; }
        }

        public int NumTries
        {
            get { return _numTries; }
            set { _numTries = value; }
        }

        private Prompt() { }  // for JSON deserialization

        public Prompt(PromptType type, string message, PromptResponseType responseType)
        {
            _type = type;
            _message = message;
            _responseType = responseType;
        }

        public Prompt(PromptType type, string message, PromptResponseType responseType, bool requireNonEmptyResponse, int numTries)
            : this(type, message, responseType)
        {
            _requireNonEmptyResponse = requireNonEmptyResponse;
            _numTries = numTries;
        }

        public Prompt(PromptType type, string message, PromptResponseType responseType, Prompt nextPrompt, string nextPromptValue)
            : this(type, message, responseType)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Prompt(PromptType type, string message, PromptResponseType responseType, bool requireNonEmptyResponse, int numTries, Prompt nextPrompt, string nextPromptValue)
            : this(type, message, responseType, requireNonEmptyResponse, numTries)
        {
            _nextPrompt = nextPrompt;
            _nextPromptValue = nextPromptValue;
        }

        public Task<ScriptDatum> RunAsync()
        {
            return Task.Run<ScriptDatum>(async () =>
                {
                    if (_requireNonEmptyResponse)
                        _message += " (response required)";

                    string response = null;

                    int triesLeft = _numTries;

                    do
                    {
                        if (_type == PromptType.Text && _responseType == PromptResponseType.Text)
                            response = SensusServiceHelper.Get().PromptForTextInput(_message);
                        else if (_type == PromptType.Text && _responseType == PromptResponseType.Voice)
                            response = await SensusServiceHelper.Get().RecognizeSpeechAsync(_message);
                        else if (_type == PromptType.Text && _responseType == PromptResponseType.None)
                            SensusServiceHelper.Get().FlashNotification(_message);
                        else if (_type == PromptType.Voice && _responseType == PromptResponseType.Text)
                        {
                            SensusServiceHelper.Get().TextToSpeech(_message);
                            response = SensusServiceHelper.Get().PromptForTextInput(_message);
                        }
                        else if (_type == PromptType.Voice && _responseType == PromptResponseType.Voice)
                        {
                            SensusServiceHelper.Get().TextToSpeech(_message);
                            response = await SensusServiceHelper.Get().RecognizeSpeechAsync(_message);
                        }
                        else if (_type == PromptType.Voice && _responseType == PromptResponseType.None)
                            SensusServiceHelper.Get().TextToSpeech(_message);
                    }
                    while (response == null && _requireNonEmptyResponse && --triesLeft > 0);

                    return new ScriptDatum(null, DateTimeOffset.UtcNow, response);
                });
        }
    }
}
