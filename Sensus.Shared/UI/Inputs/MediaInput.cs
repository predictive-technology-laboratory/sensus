using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class MediaInput : Input
	{
        private Label _label;

		public override object Value => null;

		public override bool Enabled { get; set; }

		public override string DefaultName => "Media";

        public override View GetView(int index)
        {
            if (base.GetView(index) == null)
            {
                _label = CreateLabel(index);
                _label.VerticalTextAlignment = TextAlignment.Center;

                base.SetView(new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children = { _label }
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
