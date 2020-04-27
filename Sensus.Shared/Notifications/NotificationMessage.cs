using System;
using System.ComponentModel;

namespace Sensus.Notifications
{
	public class NotificationMessage : INotifyPropertyChanged
	{
		public string Id { get; set; }
		public string ProtocolId { get; set; }
		public string Title { get; set; }
		public string Message { get; set; }
		public DateTime ReceivedOn { get; set; }
		public DateTime? ViewedOn { get; set; }

		public string DisplayTitle { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		public Protocol Protocol { get; set; }

		public void SetAsViewed()
		{
			if (ViewedOn == null)
			{
				ViewedOn = DateTime.Now;

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewedOn)));
			}
		}
	}
}
