using System;

namespace Sensus
{
	public class MessageEventDatum : Datum
	{
		public MessageEventDatum(string messageId, string protocolId, string eventType, DateTimeOffset timestamp) : base(timestamp)
		{
			MessageId = messageId;
			ProtocolId = protocolId;
			EventType = eventType;
		}

		public override string DisplayDetail => EventType;

		public override object StringPlaceholderValue => EventType;

		/// <summary>
		/// The id of the message this event is associated with.
		/// </summary>
		public string MessageId { get; set; }
		/// <summary>
		/// The type of event.
		/// </summary>
		public string EventType { get; set; }
	}
}
