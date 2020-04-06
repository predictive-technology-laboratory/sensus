using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class MediaInput : Input
	{
        private struct MediaEvent
        {
            public DateTimeOffset Timestamp { get; set; }
            public int Position { get; set; }
            public string Event { get; set; }
        }

        private List<MediaEvent> _events;
        private Label _label;

		public override object Value
        {
            get
            {
                return _events;
            }
        }

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

                _events = new List<MediaEvent>();

                if (Media.Type.ToLower().StartsWith("image"))
                {
                    _events.Add(new MediaEvent { Timestamp = DateTimeOffset.UtcNow, Position = 0, Event = "View" });

                    Complete = true;
                }
                else if (Media.Type.ToLower().StartsWith("video"))
                {
                    // the MediaView should be set after SetView is called
                    MediaView.VideoEvent += (o, e) =>
                    {
                        _events.Add(new MediaEvent { Timestamp = DateTimeOffset.UtcNow, Position = (int)e.Position.TotalMilliseconds, Event = e.Event });

                        if (e.Event == VideoPlayer.END)
                        {
                            Complete = true;
                        }
                    };
                }
            }
            else
            {
                _label.Text = GetLabelText(index);  // if the view was already initialized, just update the label since the index might have changed.
            }

            return base.GetView(index);
        }
    }
}
