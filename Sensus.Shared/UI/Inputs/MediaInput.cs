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

using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		private MediaView _mediaView;

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

				_mediaView = new MediaView()
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand
				};

				base.SetView(new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					Children = { _label, _mediaView }
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
					_mediaView.VideoEvent += (o, e) =>
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

		[MediaPickerUiProperty("Media:", true, 7)]
		public MediaObject Media { get; set; }

		public bool HasMedia
		{
			get
			{
				return Media != null && string.IsNullOrWhiteSpace(Media.Data) == false;
			}
		}

		public async Task InitializeMediaAsync()
		{
			if (HasMedia)
			{
				await _mediaView.SetMediaAsync(Media);
			}
		}

		public async Task DisposeMediaAsync()
		{
			if (HasMedia)
			{
				await _mediaView.DisposeMediaAsync();
			}
		}
	}
}
