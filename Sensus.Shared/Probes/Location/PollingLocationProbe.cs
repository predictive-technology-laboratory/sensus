//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
                return 15000; // every 15 seconds
            }
        }

        public sealed override Type DatumType
        {
            get { return typeof(LocationDatum); }
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
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
