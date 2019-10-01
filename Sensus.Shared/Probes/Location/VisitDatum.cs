using System;

namespace Sensus.Probes.Location
{
	public class VisitDatum : Datum
	{
		private DateTimeOffset? _arrivalDate;
		private DateTimeOffset? _departureDate;
		private double _latitude;
		private double _longitude;
		private double _horizontalAccuracy;

		public override string DisplayDetail
		{
			get
			{
				return Math.Round(Latitude, 2) + " (lat), " + Math.Round(Longitude, 2) + " (lon)";
			}
		}

		public override object StringPlaceholderValue
		{
			get
			{
				return DisplayDetail;
			}
		}

		public VisitDatum(DateTimeOffset? arrivalDate, DateTimeOffset? departureDate, double latitude, double longitude, double horizontalAccuracy, DateTimeOffset timestamp) : base(timestamp)
		{
			_arrivalDate = arrivalDate;
			_departureDate = departureDate;
			_latitude = latitude;
			_longitude = longitude;
			_horizontalAccuracy = horizontalAccuracy;
		}

		public DateTimeOffset? ArrivalDate
		{
			get
			{
				return _arrivalDate;
			}
			set
			{
				_arrivalDate = value;
			}
		}
		public DateTimeOffset? DepartureDate
		{
			get
			{
				return _departureDate;
			}
			set
			{
				_departureDate = value;
			}
		}
		public double Latitude
		{
			get
			{
				return _latitude;
			}
			set
			{
				_latitude = value;
			}
		}
		public double Longitude
		{
			get
			{
				return _longitude;
			}
			set
			{
				_longitude = value;
			}
		}
		public double HorizontalAccuracy
		{
			get
			{
				return _horizontalAccuracy;
			}
			set
			{
				_horizontalAccuracy = value;
			}
		}
	}
}