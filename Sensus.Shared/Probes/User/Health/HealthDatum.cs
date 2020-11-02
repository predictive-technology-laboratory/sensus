using System;

namespace Sensus.Probes.User.Health
{
	public class HealthDatum : Datum
	{
		public override string DisplayDetail => $"{DataType}: {Value} {Unit}";

		public override object StringPlaceholderValue => Value;

		public HealthDatum(string dataType, string value, string unit, string source, DateTime startDate, DateTime endDate, DateTimeOffset timestamp) : base(timestamp)
		{
			DataType = dataType;
			Value = value;
			Unit = unit;
			Source = source;
			StartDate = startDate;
			EndDate = endDate;
		}

		public string DataType { get; set; }
		public string Value { get; set; }
		public string Unit { get; set; }
		public string Source { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
	}
}
