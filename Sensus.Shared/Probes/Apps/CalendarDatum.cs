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
		private string _start;
		private string _end;
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

		public CalendarDatum(string eventId, string start, string end, double duration, string description, string eventLocation, string organizer, bool isOrganizer, string title, DateTimeOffset timestamp) : base(timestamp)
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
		public string Start
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
		public string End
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
	}
}
