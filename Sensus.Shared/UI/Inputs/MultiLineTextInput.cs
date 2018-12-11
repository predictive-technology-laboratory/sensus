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

using Xamarin.Forms;
using Newtonsoft.Json;
using Sensus.UI.UiProperties;

namespace Sensus.UI.Inputs
{
    public class MultiLineTextInput : Input
    {
        private Editor _editor;
        private Keyboard _keyboard;
        private Label _label;
        private bool _hasFocused;
        private int _height;

        /// <summary>
        /// Height in pixels of the multi-line text input.
        /// </summary>
        /// <value>The height.</value>
        [EntryIntegerUiProperty(null, true, 5, true)]
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (value < 100)
                {
                    value = 100;
                }

                _height = value;
            }
        }

        public override object Value
        {
            get
            {
                return _editor == null || !_hasFocused || string.IsNullOrWhiteSpace(_editor.Text) ? null : _editor.Text;
            }
        }

        [JsonIgnore]
        public override bool Enabled
        {
            get
            {
                return _editor.IsEnabled;
            }
            set
            {
                _editor.IsEnabled = value;
            }
        }

        public override string DefaultName
        {
            get
            {
                return "Multi-Line Text Entry";
            }
        }

        public MultiLineTextInput()
        {
        }

        public MultiLineTextInput(string labelText, Keyboard keyboard)
            : base(labelText)
        {
            Construct(keyboard);
        }

        public MultiLineTextInput(string labelText, string name, Keyboard keyboard)
            : base(labelText, name)
        {
            Construct(keyboard);
        }

        private void Construct(Keyboard keyboard)
        {
            _keyboard = keyboard;
            _height = 100;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _editor = new Editor
                {
                    Text = "Provide response here.",
                    FontSize = 20,
                    Keyboard = _keyboard,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = _height

                    // set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
                    , StyleId = Name
#endif
                };

                Color defaultTextColor = _editor.TextColor;
                _editor.TextColor = Color.Gray;
                _hasFocused = false;
                _editor.Focused += (o, e) =>
                {
                    if (!_hasFocused)
                    {
                        _editor.Text = "";
                        _editor.TextColor = defaultTextColor;
                        _hasFocused = true;
                    }
                };

                _editor.TextChanged += (o, e) =>
                {
                    Complete = Value != null;
                };

                _label = CreateLabel(index);

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { _label, _editor }
                });
            }
            else
            {
                // if the view was already initialized, just update the label since the index might have changed.
                _label.Text = GetLabelText(index);

                // if the view is not enabled, there should be no tip text since the user can't do anything with the entry.
                if (!Enabled && !_hasFocused)
                {
                    _editor.Text = "";
                }
            }

            return base.GetView(index);
        }
    }
}
