using Sensus.Context;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Xamarin.Forms;

namespace Sensus.UI.Inputs
{
	public class TimedText : Input
	{
		protected Label _textLable;
		protected Timer _timer;
		protected List<string> _textList;
		protected int _index;
		protected double _elapsed;

		public TimedText() : base()
		{
			Duration = 30;
		}

		public override object Value
		{
			get
			{
				return _textList[_index % _textList.Count];
			}
		}

		public override bool Enabled { get; set; }

		public override string DefaultName => "Timed Text";

		[EntryIntegerUiProperty("Duration", true, 10, false)]
		public int Duration { get; set; }
		[EditorUiProperty("Text", true, 11, true)]
		public object TextCollection { get; set; }

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				Label label = CreateLabel(-1);

				_textLable = new Label()
				{
					StyleClass = new List<string> { "InputContent" },
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				_textList = TextCollection as List<string>;

				_index = 0;
				_elapsed = 0;

				if (_textList == null && TextCollection is string textString)
				{
					_textList = textString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
				}

				double interval = (double)Duration / _textList.Count;

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
					Children = { label, _textLable }
				});
			}

			return base.GetView(index);
		}

		private void CycleText(double interval)
		{
			SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
			{
				_textLable.Text = _textList[_index % _textList.Count];

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

			base.OnDisappearing(result);
		}
	}
}
