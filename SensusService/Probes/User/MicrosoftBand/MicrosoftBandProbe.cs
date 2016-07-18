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
using Microsoft.Band.Portable.Sensors;
using Newtonsoft.Json;

namespace SensusService.Probes.User.MicrosoftBand
{
    public abstract class MicrosoftBandProbe<SensorType, ReadingType> : MicrosoftBandProbeBase
        where SensorType : BandSensorBase<ReadingType>
        where ReadingType : IBandSensorReading
    {
        [JsonIgnore]
        protected abstract SensorType Sensor { get; }

        protected override void ConfigureSensor()
        {
            if (Sensor == null)
                throw new Exception("Sensor not connected.");
            else
                Sensor.ReadingChanged += ReadingChanged;
        }

        private void ReadingChanged(object sender, BandSensorReadingEventArgs<ReadingType> args)
        {
            StoreDatum(GetDatumFromReading(args.SensorReading));
        }

        protected override void StartReadings()
        {
            if (Sensor == null)
                throw new Exception("Sensor not connected.");
            else
                Sensor.StartReadingsAsync(SamplingRate).Wait();
        }

        protected abstract Datum GetDatumFromReading(ReadingType reading);

        protected override void StopReadings()
        {
            if (Sensor != null)
                Sensor.StopReadingsAsync().Wait();
        }
    }
}