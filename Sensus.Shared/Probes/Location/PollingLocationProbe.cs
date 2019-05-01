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
using System.Threading;
using System.Collections.Generic;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;
using System.Linq;

namespace Sensus.Probes.Location
{
    /// <summary>
    /// Periodically takes a location reading.
    /// </summary>
    public class PollingLocationProbe : PollingProbe
    {
        public sealed override string DisplayName
        {
            get { return "GPS Location"; }
        }

        public override int DefaultPollingSleepDurationMS
        {
            get
            {
                return (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
            }
        }

        public sealed override Type DatumType
        {
            get { return typeof(LocationDatum); }
        }

        protected override async Task ProtectedInitializeAsync()
        {
            await base.ProtectedInitializeAsync();

            if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location) != PermissionStatus.Granted)
            {
                // throw standard exception instead of NotSupportedException, since the user might decide to enable GPS in the future
                // and we'd like the probe to be restarted at that time.
                string error = "Geolocation is not permitted on this device. Cannot start location probe.";
                await SensusServiceHelper.Get().FlashNotificationAsync(error);
                throw new Exception(error);
            }
        }

        protected sealed override async Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            Position currentPosition = await GpsReceiver.Get().GetReadingAsync(cancellationToken, false);

            if (currentPosition == null)
            {
                throw new Exception("Failed to get GPS reading.");
            }
            else
            {
                return new Datum[] { new LocationDatum(currentPosition.Timestamp, currentPosition.Accuracy, currentPosition.Latitude, currentPosition.Longitude) }.ToList();
            }
        }

        protected override ChartSeries GetChartSeries()
        {
            return new LineSeries();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            LocationDatum location = datum as LocationDatum;
            return new ChartDataPoint(location.Longitude, location.Latitude);
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Longitude"
                }
            };
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return new NumericalAxis
            {
                Title = new ChartAxisTitle
                {
                    Text = "Latitude"
                }
            };
        }
    }
}