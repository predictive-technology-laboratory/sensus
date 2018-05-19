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

        public QrCodeInput(string qrCodePrefix, string barcodeValueLabelTextPrefix, string labelText)
            : base(labelText)
        {
            _qrCodePrefix = qrCodePrefix;
            _barcodeValueLabelTextPrefix = barcodeValueLabelTextPrefix;
        }

        public QrCodeInput(string qrCodePrefix, string barcodeValueLabelTextPrefix, string labelText, string name)
            : base(labelText, name)
        {
            _qrCodePrefix = qrCodePrefix;
            _barcodeValueLabelTextPrefix = barcodeValueLabelTextPrefix;
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
                    _scanResult = await SensusServiceHelper.Get().ScanQrCodeAsync(_qrCodePrefix, Application.Current.MainPage.Navigation);

                    Complete = Value != null;

                    codeLabel.Text = _barcodeValueLabelTextPrefix + Value?.ToString();
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
