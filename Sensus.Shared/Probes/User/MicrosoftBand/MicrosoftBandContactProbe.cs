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
using Syncfusion.SfChart.XForms;
using System.Threading.Tasks;

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandContactProbe : MicrosoftBandProbe<BandContactSensor, BandContactReading>
    {
        public event EventHandler<ContactState> ContactStateChanged;

        private ContactState? _previousState;

        public override Type DatumType
        {
            get
            {
                return typeof(MicrosoftBandContactDatum);
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Microsoft Band Contact";
            }
        }

        protected override BandContactSensor GetSensor(BandClient bandClient)
        {
            return bandClient.SensorManager.Contact;
        }

        protected override void ReadingChangedAsync(object sender, BandSensorReadingEventArgs<BandContactReading> args)
        {
            try
            {
                ContactState state = args.SensorReading.State;

                if (_previousState == null || state != _previousState.Value)
                {
                    ContactStateChanged?.Invoke(this, state);
                    _previousState = state;
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Error processing Band contact change:  " + ex.Message, LoggingLevel.Normal, GetType());
            }

            base.ReadingChangedAsync(sender, args);
        }

        protected override Datum GetDatumFromReading(BandContactReading reading)
        {
            return new MicrosoftBandContactDatum(DateTimeOffset.UtcNow, reading.State);
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            return null;
        }
    }
}
