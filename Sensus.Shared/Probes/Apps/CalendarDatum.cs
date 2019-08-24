using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
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
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public double Duration { get; set; }
        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Description { get; set; }
        public string EventLocation { get; set; }
        [StringProbeTriggerProperty]
        [Anonymizable(null, typeof(StringHashAnonymizer), false)]
        public string Organizer { get; set; }
        public bool IsOrganizer { get; set; }

        public override string DisplayDetail => throw new NotImplementedException();

        public override object StringPlaceholderValue => throw new NotImplementedException();
    }
}
