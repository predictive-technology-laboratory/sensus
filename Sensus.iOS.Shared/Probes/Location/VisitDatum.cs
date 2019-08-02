using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.iOS.Probes.Location
{
	public class VisitDatum : Datum
	{
		public override string DisplayDetail => throw new NotImplementedException();

		public override object StringPlaceholderValue => throw new NotImplementedException();

		public DateTimeOffset ArrivalDate { get; set; }
		public DateTimeOffset DepartureDate { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double HorizontalAccuracy { get; set; }

		public VisitDatum(DateTimeOffset arrivalDate, DateTimeOffset departureDate, double latitude, double longitude, double horizontalAccuracy, DateTimeOffset timestamp) : base(timestamp)
		{
			ArrivalDate = arrivalDate;
			DepartureDate = departureDate;
			Latitude = latitude;
			Longitude = longitude;
			HorizontalAccuracy = horizontalAccuracy;
		}
	}
}
