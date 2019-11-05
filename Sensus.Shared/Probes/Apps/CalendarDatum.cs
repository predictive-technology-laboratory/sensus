using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using System;

namespace Sensus.Probes.Apps
{
	public class CalendarDatum : Datum
	{
		private string _eventId;
		private string _title;
		private DateTimeOffset _start;
		private DateTimeOffset _end;
		private double _duration;
		private string _description;
		private string _eventLocation;
		private string _organizer;
		private bool _isOrganizer;

		/// <summary>
		/// For JSON deserialization.
		/// </summary>
		public CalendarDatum()
		{

		}

		public CalendarDatum(string eventId, string title, DateTimeOffset start, DateTimeOffset end, double duration, string description, string eventLocation, string organizer, bool isOrganizer, DateTimeOffset timestamp) : base(timestamp)
		{
			_eventId = eventId;
			_title = title;
			_start = start;
			_end = end;
			_duration = duration;
			_description = description;
			_eventLocation = eventLocation;
			_organizer = organizer;
			_isOrganizer = isOrganizer;
		}

		/// <summary>
		/// The event id.
		/// </summary>
		public string EventId
		{
			get
			{
				return _eventId;
			}
			set
			{
				_eventId = value;
			}
		}
        /// <summary>
        /// The title of the event.
        /// </summary>
        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
			}
		}
		/// <summary>
		/// The date and time the event starts.
		/// </summary>
		public DateTimeOffset Start
		{
			get
			{
				return _start;
			}
			set
			{
				_start = value;
			}
		}
		/// <summary>
		/// The date and time the event ends.
		/// </summary>
		public DateTimeOffset End
		{
			get
			{
				return _end;
			}
			set
			{
				_end = value;
			}
		}
		/// <summary>
		/// The duration of the event.
		/// </summary>
		public double Duration
		{
			get
			{
				return _duration;
			}
			set
			{
				_duration = value;
			}
		}
		/// <summary>
		/// The description of the event.
		/// </summary>
		[StringProbeTriggerProperty]
		[Anonymizable(null, typeof(StringHashAnonymizer), false)]
		public string Description
		{
			get
			{
				return _description;
			}
			set
			{
				_description = value;
			}
		}
		/// <summary>
		/// The location of the event.
		/// </summary>
		public string EventLocation
		{
			get
			{
				return _eventLocation;
			}
			set
			{
				_eventLocation = value;
			}
		}
		/// <summary>
		/// The organizer of the event.
		/// </summary>
		[StringProbeTriggerProperty]
		[Anonymizable(null, typeof(StringHashAnonymizer), false)]
		public string Organizer
		{
			get
			{
				return _organizer;
			}
			set
			{
				_organizer = value;
			}
		}
		/// <summary>
		/// Indicates if the Sensus user is the organizer.
		/// </summary>
		public bool IsOrganizer
		{
			get
			{
				return _isOrganizer;
			}
			set
			{
				_isOrganizer = value;
			}
		}

		public override string DisplayDetail
		{
			get
			{
				return "(Calendar data)";
			}
		}

		public override object StringPlaceholderValue
		{
			get
			{
				return "(Calendar data)";
			}
		}

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine + $"{_title} from {_start} to {_end}";
		}
	}
}
