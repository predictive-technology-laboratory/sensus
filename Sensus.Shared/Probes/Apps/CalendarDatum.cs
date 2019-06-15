using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Apps
{
    public class CalendarDatum : Datum
    {
        public CalendarDatum()
        {

        }

        public CalendarDatum(DateTimeOffset timeStamp)
            : base(timeStamp)
        {

        }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public double Duration { get; set; }
        public string Description { get; set; }
        public string EventLocation { get; set; }
        public string Organizer { get; set; }
        public bool IsOrganizer { get; set; }

        public override string DisplayDetail => throw new NotImplementedException();

        public override object StringPlaceholderValue => throw new NotImplementedException();
    }
}
