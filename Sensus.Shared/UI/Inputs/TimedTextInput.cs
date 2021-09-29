using Newtonsoft.Json;
using Sensus.Context;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class TimedTextInput : Input
	{
		protected Label _textLabel;
		protected Timer _timer;
		protected int _index;
		protected double _elapsed;

		public TimedTextInput() : base()
		{
			Duration = 30;
			CenterText = true;
		}

		public override object Value
		{
			get
			{
				return Text;
			}
		}

		public override bool Enabled => true;

		public override string DefaultName => "Timed Text";

		[EntryIntegerUiProperty("Duration:", true, 10, false)]
		public int Duration { get; set; }
		[EditableListUiProperty("Text:", true, 11, true)]
		public List<string> Text { get; set; }
		[OnOffUiProperty("Center Text:", true, 12)]
		public bool CenterText { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Label label = CreateLabel(-1);

				_textLabel = new Label()
				{
					StyleClass = new List<string> { "InputContent" },
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				if (CenterText)
				{
					_textLabel.HorizontalTextAlignment = TextAlignment.Center;
				}

				_index = 0;
				_elapsed = 0;

				double interval = (double)Duration / Text.Count;

				_timer = new Timer(interval * 1000);

				_timer.Elapsed += (s, e) =>
				{
					CycleText(interval);
				};

				CycleText(interval);

				_timer.Start();

				base.SetView(new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					Children = { label, _textLabel }
				});
			}

			return base.GetView(index);
		}

		private void CycleText(double interval)
		{
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
			{
				_textLabel.Text = Text[_index % Text.Count];

				_index += 1;
				_elapsed += interval;

				if (_elapsed >= Duration)
				{
					Complete = true;
				}
			});
		}

		public override void OnDisappearing(InputGroupPage.NavigationResult result)
		{
			_timer.Stop();
			_timer.Dispose();

			base.OnDisappearing(result);
		}
	}
}
