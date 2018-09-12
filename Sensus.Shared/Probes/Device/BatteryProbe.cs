// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;
using System.Linq;

namespace Sensus.Probes.Device
{
    /// <summary>
    /// Obtains the device's current battery level on the scale of [0,100] percent.
    /// </summary>
    public class BatteryProbe : PollingProbe
    {
        public sealed override string DisplayName
        {
            get { return "Battery Level"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return (int)TimeSpan.FromHours(1).TotalMilliseconds;
            }
        }

        public sealed override Type DatumType
        {
            get { return typeof(BatteryDatum); }
        }

        protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new Datum[] { new BatteryDatum(DateTimeOffset.UtcNow, SensusServiceHelper.Get().BatteryChargePercent) }.ToList());
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as BatteryDatum).Level);
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return new DateTimeAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Time"
                }
            };
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Level (%)"
                }
            };
        }
    }
}
