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
using ZXing;

namespace Sensus.UI.Inputs
{
    public class QrCodeInput : Input
    {
        private Label _label;
        private Button _button;
        private string _scanResult;
        private string _barcodeValueLabelTextPrefix;
        private string _qrCodePrefix;
        private bool _masked;

        public override object Value
        {
            get { return string.IsNullOrWhiteSpace(_scanResult) ? null : _scanResult; }
        }

        public override bool Enabled
        {
            get
            {
                return _button.IsEnabled;
            }
            set
            {
                _button.IsEnabled = value; 
            }
        }

        public override string DefaultName
        {
            get { return "QR Code"; }
        }
    
        public QrCodeInput()
        {
        }

        public QrCodeInput(string qrCodePrefix, string barcodeValueLabelTextPrefix, bool masked, string labelText)
            : base(labelText)
        {
            _qrCodePrefix = qrCodePrefix;
            _barcodeValueLabelTextPrefix = barcodeValueLabelTextPrefix;
            _masked = masked;
        }

        public QrCodeInput(string qrCodePrefix, string barcodeValueLabelTextPrefix, bool masked, string labelText, string name)
            : base(labelText, name)
        {
            _qrCodePrefix = qrCodePrefix;
            _barcodeValueLabelTextPrefix = barcodeValueLabelTextPrefix;
            _masked = masked;
        }

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _button = new Button
                {
                    Text = "Scan Barcode",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand

                    // set the style ID on the view so that we can retrieve it when UI testing
#if UI_TESTING
                    , StyleId = Name
#endif
                };

                _label = CreateLabel(index);

                Label codeLabel = new Label
                {
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Text = _barcodeValueLabelTextPrefix
                };

                _button.Clicked += async (o, e) =>
                {
                    _scanResult = await SensusServiceHelper.Get().ScanQrCodeAsync(_qrCodePrefix);

                    Complete = Value != null;

                    // make the displayed value if needed
                    string displayValue = Value?.ToString();
                    if (displayValue != null && _masked)
                    {
                        displayValue = new string('*', displayValue.Length);
                    }

                    codeLabel.Text = _barcodeValueLabelTextPrefix + displayValue;
                };

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    VerticalOptions = LayoutOptions.Start,
                    Children = { _label, _button, codeLabel }
                });
            }
            else
            {
                _label.Text = GetLabelText(index);  // if the view was already initialized, just update the label since the index might have changed.
            }

            return base.GetView(index);
        }
    }
}
