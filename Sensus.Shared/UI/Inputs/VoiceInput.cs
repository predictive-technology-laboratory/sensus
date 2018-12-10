//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
        [EntryStringUiProperty("Output Message:", true, 11, true)]
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
        [EntryStringUiProperty("Define Variable:", true, 2, false)]
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

        public async Task<string> RunAsync(DateTimeOffset? firstRunTimestamp, Action postDisplayCallback)
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
        }
    }
}
