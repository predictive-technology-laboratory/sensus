using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
    public abstract class CalendarProbe : PollingProbe
    {
        public override int DefaultPollingSleepDurationMS => throw new NotImplementedException();

        protected abstract Task<List<Datum>> GetCalendarEvents();

        public override string DisplayName => throw new NotImplementedException();

        public override Type DatumType => throw new NotImplementedException();

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }
       
        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
