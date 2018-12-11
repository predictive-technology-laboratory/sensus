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
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.Probes.User.MicrosoftBand
{
    public abstract class MicrosoftBandProbe<SensorType, ReadingType> : MicrosoftBandProbeBase
        where SensorType : BandSensorBase<ReadingType>
        where ReadingType : IBandSensorReading
    {
        private SensorType _sensor;

        protected SensorType Sensor
        {
            get
            {
                return _sensor;
            }
        }

        protected override async Task ConfigureAsync(BandClient bandClient)
        {
            SensorType bandClientSensor = GetSensor(bandClient);

            // if we're currently configured with the sensor from the client, do nothing
            if (ReferenceEquals(_sensor, bandClientSensor))
            {
                return;
            }

            // if we have a sensor that isn't from the client, tear it down so that we do not continue to receive readings from it.
            if (_sensor != null)
            {
                // disconnect reading callback
                try
                {
                    _sensor.ReadingChanged -= ReadingChangedAsync;
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to remove reading-changed handler when reconfiguring probe:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                // unregister listener
                try
                {
                    await _sensor.StopReadingsAsync();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to stop readings when reconfiguring probe:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            }

            // set up the new sensor
            _sensor = bandClientSensor;
            _sensor.ReadingChanged += ReadingChangedAsync;
        }

        protected abstract SensorType GetSensor(BandClient bandClient);

        protected virtual async void ReadingChangedAsync(object sender, BandSensorReadingEventArgs<ReadingType> args)
        {
            try
            {
                Datum datum = GetDatumFromReading(args.SensorReading);
                await StoreDatumAsync(datum);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Error getting/storing Band datum:  " + ex.Message, LoggingLevel.Normal, GetType());
            }
        }

        protected override async Task StartReadingsAsync()
        {
            if (_sensor == null)
            {
                throw new Exception("Sensor not configured.");
            }
            else
            {
                await _sensor.StartReadingsAsync(SamplingRate);
            }
        }

        protected abstract Datum GetDatumFromReading(ReadingType reading);

        protected override async Task StopReadingsAsync()
        {
            if (_sensor != null)
            {
                await _sensor.StopReadingsAsync();
            }
        }
    }
}
