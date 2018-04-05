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

using System;
using Xamarin.Forms;
using Newtonsoft.Json;
using Sensus.Exceptions;
using Sensus.UI.UiProperties;
using System.Threading.Tasks;

namespace Sensus.UI.Inputs
{
    public class VoiceInput : Input, IVariableDefiningInput
    {
        private string _outputMessage;
        private string _response;
        private bool _enabled;
        private string _definedVariable;

        /// <summary>
        /// Message to generate speech for when displaying this input.
        /// </summary>
        /// <value>The output message.</value>
        [EntryStringUiProperty("Output Message:", true, 11)]
        public string OutputMessage
        {
            get { return _outputMessage; }
            set { _outputMessage = value; }
        }

        /// <summary>
        /// The name of the variable in <see cref="Protocol.VariableValueUiProperty"/> that this input should
        /// define the value for. For example, if you wanted this input to supply the value for a variable
        /// named `study-name`, then set this field to `study-name` and the user's selection will be used as
        /// the value for this variable. 
        /// </summary>
        /// <value>The defined variable.</value>
        [EntryStringUiProperty("Define Variable:", true, 2)]
        public string DefinedVariable
        {
            get
            {
                return _definedVariable;
            }
            set
            {
                _definedVariable = value?.Trim();
            }
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
            Construct("");
        }

        public VoiceInput(string outputMessage)
            : base()
        {
            Construct(outputMessage);
        }

        public VoiceInput(string name, string outputMessage)
            : base(null, name)
        {
            Construct(outputMessage);
        }

        private void Construct(string outputMessage)
        {
            _enabled = true;
            _outputMessage = outputMessage;
        }

        public override View GetView(int index)
        {
            return null;
        }

        protected override void SetView(View value)
        {
            SensusException.Report("Cannot set View on VoiceInput.");
        }

        public Task<string> RunAsync(DateTimeOffset? firstRunTimestamp, Action postDisplayCallback)
        {
            return Task.Run(async () =>
            {
                await SensusServiceHelper.Get().TextToSpeechAsync(_outputMessage);

                _response = await SensusServiceHelper.Get().RunVoicePromptAsync(_outputMessage, postDisplayCallback);

                Viewed = true;

                if (string.IsNullOrWhiteSpace(_response))
                {
                    _response = null;
                }

                Complete = _response != null;

                return _response;
            });
        }
    }
}