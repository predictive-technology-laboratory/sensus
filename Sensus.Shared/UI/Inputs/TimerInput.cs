using Newtonsoft.Json;
using Sensus.Context;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Timers;
using Xamarin.Forms;
using static Sensus.UI.InputGroupPage;

namespace Sensus.UI.Inputs
{
	public class TimerInput : Input, IVariableDefiningInput
	{
		private const string STARTED = "Started";
		private const string USER_STARTED = "UserStarted";
		private const string USER_PAUSED = "UserPaused";
		private const string USER_STOPPED = "UserStopped";
		private const string USER_SUBMITTED = "UserSubmitted";


		private struct TimerEvent
		{
			public DateTimeOffset Timestamp { get; set; }
			public int ElapsedTime { get; set; }
			public string Event { get; set; }
		}

		private const int DISPLAY_PRECISION = 1000;

		private Label _label;
		private Label _timerDisplay;
		private string _definedVariable;
		private int _elapsedTime;
		private Timer _timer;
		private List<TimerEvent> _events;
		private Button _startButton;
		private Button _pauseButton;
		private Button _stopButton;
		/// <summary>
		/// The name of the variable in <see cref="Protocol.VariableValueUiProperty"/> that this input should
		/// define the value for. For example, if you wanted this input to supply the value for a variable
		/// named `study-name`, then set this field to `study-name` and the user's selection will be used as
		/// the value for this variable. 
		/// </summary>
		/// <value>The defined variable.</value>
		[EntryStringUiProperty("Define Variable:", true, 15, false)]
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
				return _events;
			}
		}

		[JsonIgnore]
		public override bool Enabled { get; set; }

		public override string DefaultName
		{
			get
			{
				return "Timer";
			}
		}

		public TimerInput()
		{
			_timer = new Timer(1);

			_timer.Elapsed += TimerElapsed;
		}

		private void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			/*SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(*/
			Device.BeginInvokeOnMainThread(() =>
			{
				_elapsedTime += 1;

				if (Duration > 0 && _elapsedTime == Duration)
				{
					_timer.Stop();

					_events.Add(new TimerEvent { Timestamp = DateTimeOffset.UtcNow, ElapsedTime = _elapsedTime, Event = "Stopped" });

					if (_startButton != null)
					{
						_startButton.IsEnabled = false;
					}

					if (_pauseButton != null)
					{
						_pauseButton.IsEnabled = false;
					}

					if (_stopButton != null)
					{
						_stopButton.IsEnabled = false;
					}

					Complete = true;
				}

				if (_elapsedTime % DISPLAY_PRECISION == 0)
				{
					_timerDisplay.Text = GetDisplayTime();
				}
			});
		}

		public TimerInput(string labelText)
			: base(labelText)
		{
		}

		public TimerInput(string labelText, string name)
			: base(labelText, name)
		{
		}

		private string GetDisplayTime()
		{
			return TimeSpan.FromMilliseconds(Math.Abs(Duration - _elapsedTime)).ToString();
		}

		[EntryIntegerUiProperty("Duration (MS):", true, 16, false)]
		public int Duration { get; set; }
		[OnOffUiProperty("Show Start Button:", true, 17)]
		public bool ShowStartButton { get; set; }
		[OnOffUiProperty("Show Pause Button:", true, 18)]
		public bool ShowPauseButton { get; set; }
		[OnOffUiProperty("Show Stop Button:", true, 19)]
		public bool ShowStopButton { get; set; }

		public override void OnDisappearing(NavigationResult result)
		{
			if (_timer.Enabled)
			{
				_timer.Stop();

				if (result == NavigationResult.Submit)
				{
					_events.Add(new TimerEvent { Timestamp = DateTimeOffset.UtcNow, ElapsedTime = _elapsedTime, Event = USER_SUBMITTED });

					Complete = true;
				}
			}
		}

		public override View GetView(int index)
		{
			if (base.GetView(index) == null)
			{
				_elapsedTime = 0;
				_events = new List<TimerEvent>();

				_label = CreateLabel(index);
				_label.VerticalTextAlignment = TextAlignment.Center;

				_timerDisplay = new Label
				{
					FontSize = 40,
					HorizontalOptions = LayoutOptions.Center,
					Text = GetDisplayTime()
				};

				StackLayout controlButtons = new StackLayout
				{
					Orientation = StackOrientation.Horizontal,
					HorizontalOptions = LayoutOptions.FillAndExpand
				};

				if (ShowStartButton || ShowPauseButton)
				{
					_startButton = new Button
					{
						HorizontalOptions = LayoutOptions.FillAndExpand,
						Text = "Start"
					};

					_startButton.Clicked += (s, e) =>
					{
						_timer.Start();

						_events.Add(new TimerEvent { Timestamp = DateTimeOffset.UtcNow, ElapsedTime = _elapsedTime, Event = USER_STARTED });

						_startButton.IsEnabled = false;

						if (_pauseButton != null)
						{
							_pauseButton.IsEnabled = true;
						}

						if (_stopButton != null)
						{
							_stopButton.IsEnabled = true;
						}
					};

					controlButtons.Children.Add(_startButton);
				}
				
				if (ShowStartButton == false)
				{
					_timer.Start();

					_events.Add(new TimerEvent { Timestamp = DateTimeOffset.UtcNow, ElapsedTime = _elapsedTime, Event = STARTED });
				}

				if (ShowPauseButton)
				{
					_pauseButton = new Button
					{
						HorizontalOptions = LayoutOptions.FillAndExpand,
						Text = "Pause"
					};

					if (ShowStartButton)
					{
						_pauseButton.IsEnabled = false;
					}

					_pauseButton.Clicked += (s, e) =>
					{
						_timer.Stop();

						_events.Add(new TimerEvent { Timestamp = DateTimeOffset.UtcNow, ElapsedTime = _elapsedTime, Event = USER_PAUSED });

						_startButton.IsEnabled = true;
						_pauseButton.IsEnabled = false;
					};

					controlButtons.Children.Add(_pauseButton);
				}

				if (ShowStopButton)
				{
					_stopButton = new Button
					{
						HorizontalOptions = LayoutOptions.FillAndExpand,
						Text = "Stop"
					};

					if (ShowStartButton)
					{
						_stopButton.IsEnabled = false;
					}

					_stopButton.Clicked += (s, e) =>
					{
						_timer.Stop();

						_events.Add(new TimerEvent { Timestamp = DateTimeOffset.UtcNow, ElapsedTime = _elapsedTime, Event = USER_STOPPED });

						_stopButton.IsEnabled = false;

						Complete = true;

						if (_startButton != null)
						{
							_startButton.IsEnabled = false;
						}

						if (_pauseButton != null)
						{
							_pauseButton.IsEnabled = false;
						}
					};

					controlButtons.Children.Add(_stopButton);
				}

				base.SetView(new StackLayout
				{
					Orientation = StackOrientation.Vertical,
					VerticalOptions = LayoutOptions.FillAndExpand,
					Children = { _label, _timerDisplay, controlButtons }
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
