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

using SensusService.Probes.Location;
using System;
using System.Collections.Generic;
using System.Threading;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Syncfusion.SfChart.XForms;

namespace SensusService.Probes.Movement
{
    public class PollingSpeedProbe : PollingProbe
    {
        private Position _previousPosition;

        private readonly object _previousPositionLocker = new object();

        public sealed override string DisplayName
        {
            get { return "Speed"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(SpeedDatum); }
        }

        public sealed override int DefaultPollingSleepDurationMS
        {
            get
            {
                return 15000; // every 15 seconds
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start speed probe.";
                SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected override void InternalStart()
        {
            // reset previous position before starting the base-class poller so it doesn't race to grab a stale previous location.
            _previousPosition = null;

            base.InternalStart();
        }

        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            SpeedDatum datum = null;

            Position currentPosition = GpsReceiver.Get().GetReading(cancellationToken);

            if (currentPosition == null)
                throw new Exception("Failed to get GPS reading.");
            else
            {
                lock (_previousPositionLocker)
                {
                    if (_previousPosition == null)
                        _previousPosition = currentPosition;
                    else if (currentPosition.Timestamp > _previousPosition.Timestamp)  // it has happened (rarely) that positions come in out of order...drop any such positions.
                    {
                        datum = new SpeedDatum(currentPosition.Timestamp, _previousPosition, currentPosition);
                        _previousPosition = currentPosition;
                    }
                }
            }

            if (datum == null)
                return new Datum[] { };  // datum will be null on the first poll and where polls return locations out of order (rare). these should count toward participation.
            else
                return new Datum[] { datum };
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return new ChartDataPoint(datum.Timestamp.LocalDateTime, (datum as SpeedDatum).KPH);
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
                    Text = "Speed (KPH)"
                }
            };
        }
    }
}