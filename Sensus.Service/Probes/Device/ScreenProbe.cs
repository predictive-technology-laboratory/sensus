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
using Syncfusion.SfChart.XForms;

namespace SensusService.Probes.Device
{
    /// <summary>
    /// Probes information about screen on/off status.
    /// </summary>
    public abstract class ScreenProbe : PollingProbe
    {
        public sealed override string DisplayName
        {
            get { return "Screen On/Off"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 15000; // screens usually stay on for a minimum of 15 seconds or so. poll at this interval to catch changes.
            }
        }

        public sealed override Type DatumType
        {
            get { return typeof(ScreenDatum); }
        }

        protected override ChartSeries GetChartSeries()
        {
            return new StepLineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as ScreenDatum).On ? 1 : 0);
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
                    Text = "On/Off"
                }
            };
        }
    }
}