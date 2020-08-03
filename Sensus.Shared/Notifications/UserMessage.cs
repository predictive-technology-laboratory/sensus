using System;
using System.ComponentModel;
using System.Threading;

namespace Sensus.Notifications
{
	public class UserMessage : INotifyPropertyChanged
	{
		public string Id { get; set; }
		public string ProtocolId { get; set; }
		public string Title { get; set; }
		public string Message { get; set; }
		public DateTime ReceivedOn { get; set; }
		public DateTime? ViewedOn { get; set; }

		public string DisplayTitle
		{
			get
			{
				return Title + $" ({Protocol?.Name ?? "No Protocol"})";
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public Protocol Protocol { get; set; }

		public void SetAsViewed()
		{
			if (ViewedOn == null)
			{
				ViewedOn = DateTime.Now;

				if (Protocol != null)
				{
					Protocol.LocalDataStore.WriteDatum(new MessageEventDatum(Id, ProtocolId, MessageEventDatum.VIEW_EVENT, DateTimeOffset.UtcNow), CancellationToken.None);
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewedOn)));
			}
		}
	}
}
