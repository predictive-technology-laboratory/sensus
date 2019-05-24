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

			StackLayout layout = new StackLayout()
			{
				Orientation = StackOrientation.Vertical,
				VerticalOptions = LayoutOptions.FillAndExpand,
				Children = { protocolList.GetView(1), pauseButton, snoozeButton, cancelButton }
			};

			Content = layout;
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
