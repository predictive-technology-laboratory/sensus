using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class PauseProtocolsPage : ContentPage
	{
		private const int DEFAULT_SNOOZE = 60;
		private TaskCompletionSource<bool> _responseTaskCompletionSource;
		public Task<bool> Result => _responseTaskCompletionSource.Task;

		private async Task PauseProtocolsAsync(IEnumerable<Protocol> protocols, DateTime? resumeTimestamp)
		{
			foreach (Protocol protocol in protocols)
			{
				await protocol.PauseAsync();

				if (resumeTimestamp != null)
				{
					await protocol.ScheduleResumeAsync(resumeTimestamp.Value);
				}
			}

			_responseTaskCompletionSource.TrySetResult(true);
		}

		public PauseProtocolsPage(IEnumerable<Protocol> protocols)
		{
			_responseTaskCompletionSource = new TaskCompletionSource<bool>();

			Title = "Pause Protocols";

			protocols = protocols.Where(x => x.AllowPause);

			StringBuilder protocolNames = new StringBuilder();

			LabelOnlyInput protocolList = new LabelOnlyInput()
			{
				LabelText = protocols.Aggregate(protocolNames, (sb, p) => sb.AppendLine(p.Name)).ToString(),
				Frame = true
			};

			Dictionary<string, int> resumeTimes = new Dictionary<string, int>
			{
				{ "Minute(s)", 1 },
				{ "Hour(s)", 60 },
				{ "Day(s)", 1440 }
			};

			KeyValuePair<string, int> defaultItem = resumeTimes.OrderBy(x => x.Value).TakeWhile(x => x.Value <= DEFAULT_SNOOZE).Last();
			int selectedIndex = resumeTimes.ToList().IndexOf(defaultItem);

			Label timeAmountLabel = new Label() { Text = "Snooze for:", FontSize = 20, VerticalTextAlignment = TextAlignment.Center };
			Entry timeAmountEntry = new Entry() { Keyboard = Keyboard.Numeric, Text = Math.Round((double)DEFAULT_SNOOZE / defaultItem.Value, 2).ToString(), HorizontalOptions = LayoutOptions.FillAndExpand };
			Picker timeAmountPicker = new Picker()
			{
				ItemsSource = resumeTimes.Keys.ToList(),
				SelectedIndex = selectedIndex,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};

			StackLayout timeAmountLayout = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Children = { timeAmountLabel, timeAmountEntry, timeAmountPicker }
			};

			Switch untilSwitch = new Switch();
			Label dateTimeLabel = new Label() { Text = "Or until:", FontSize = 20, VerticalTextAlignment = TextAlignment.Center };
			DatePicker datePicker = new DatePicker()
			{
				Date = DateTime.Now.Date,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				IsEnabled = false
			};
			TimePicker timePicker = new TimePicker()
			{
				Time = DateTime.Now.AddMinutes(DEFAULT_SNOOZE).TimeOfDay,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				IsEnabled = false
			};

			untilSwitch.Toggled += (s, e) =>
			{
				timeAmountEntry.IsEnabled = e.Value == false;
				timeAmountPicker.IsEnabled = e.Value == false;
				datePicker.IsEnabled = e.Value;
				timePicker.IsEnabled = e.Value;
			};

			StackLayout dateTimeLayout = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Children = { untilSwitch, dateTimeLabel, datePicker, timePicker }
			};

			//DateTimeInput dateTimeInput = new DateTimeInput()
			//{
			//	DisplayNumber = false,
			//	Required = false,
			//	LabelText = "Or until:"
			//};

			StackLayout resumeTimeLayout = new StackLayout()
			{
				Orientation = StackOrientation.Vertical,
				Children = { timeAmountLayout, dateTimeLayout }
			};

			Button pauseButton = new Button()
			{
				Text = "Pause"
			};

			pauseButton.Clicked += async (s, e) =>
			{
				await PauseProtocolsAsync(protocols, null);

				await Navigation.PopAsync();
			};

			Button snoozeButton = new Button()
			{
				Text = "Snooze"
			};

			snoozeButton.Clicked += async (s, e) =>
			{
				await PauseProtocolsAsync(protocols, DateTime.Now.AddSeconds(5));

				await Navigation.PopAsync();
			};

			Button cancelButton = new Button()
			{
				Text = "Cancel"
			};

			cancelButton.Clicked += async (s, e) =>
			{
				_responseTaskCompletionSource.TrySetResult(false);

				await Navigation.PopAsync();
			};

			BoxView divider = new BoxView { Color = Color.Gray, HorizontalOptions = LayoutOptions.FillAndExpand, HeightRequest = 0.5 };

			StackLayout layout = new StackLayout()
			{
				Orientation = StackOrientation.Vertical,
				VerticalOptions = LayoutOptions.FillAndExpand,
				Children = { protocolList.GetView(1), pauseButton, resumeTimeLayout, snoozeButton, divider, cancelButton }
			};

			Content = layout;
		}

		private void UntilSwitch_Toggled(object sender, ToggledEventArgs e)
		{
			throw new NotImplementedException();
		}

		public PauseProtocolsPage(Protocol protocol) : this(new Protocol[] { protocol })
		{

		}

		protected override bool OnBackButtonPressed()
		{
			_responseTaskCompletionSource.TrySetResult(false);

			return base.OnBackButtonPressed();
		}
	}
}
