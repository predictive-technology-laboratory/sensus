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
using Xunit;
using Sensus.Probes;
using Sensus.Probes.Movement;
using Sensus.Tests.Classes;

namespace Sensus.Tests.Probes
{
    
    public class DataRateCalculatorTests
    {
        [Fact]
        public void SampleSizeTest()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => new DataRateCalculator(-1));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => new DataRateCalculator(0));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => new DataRateCalculator(1));

            DataRateCalculator drc = new DataRateCalculator(2);
            drc = new DataRateCalculator(100);
        }

        [Fact]
        public void DataRateTest1()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 10;
            WriteData(2, nominalDataPerSecond, TimeSpan.FromSeconds(100), null, (datum, calculatedDataPerSecond, action) =>
            {                
                Assert.Equal(calculatedDataPerSecond, nominalDataPerSecond);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
            });
        }

        [Fact]
        public void DataRateTest2()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 13;
            WriteData(12, nominalDataPerSecond, TimeSpan.FromSeconds(100), null, (datum, calculatedDataPerSecond, action) =>
            {
                Assert.True(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond) <= 1);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
            });
        }

        [Fact]
        public void DataRateTest3()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 13;
            WriteData(14, nominalDataPerSecond, TimeSpan.FromSeconds(100), null, (datum, calculatedDataPerSecond, action) =>
            {
                Assert.True(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond) <= 1);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
            });
        }

        [Fact]
        public void DataRateTest4()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 47;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(100), null, (datum, calculatedDataPerSecond, action) =>
            {
                Assert.True(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond) <= 1);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
            });
        }

        [Fact]
        public void DataRateTest5()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 113;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(100), null, (datum, calculatedDataPerSecond, action) =>
            {
                Assert.True(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond) <= 1);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
            });
        }

        [Fact]
        public void DataRateTest6()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 192;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(100), null, (datum, calculatedDataPerSecond, action) =>
            {
                Assert.True(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond) <= 1);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
            });
        }

        [Fact]
        public void NeverKeepWithZeroRateLimitTest()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 192;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(100), 0, (datum, calculatedDataPerSecond, action) =>
            {
                Assert.True(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond) <= 1);
                Assert.Equal(action, DataRateCalculator.SamplingAction.Drop);
            });
        }

        [Fact]
        public void HasDataRateWithZeroRateLimitTest()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 192;
            bool receivedDataRate = false;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(100), 0, (datum, calculatedDataPerSecond, action) =>
            {
                receivedDataRate = true;
            });

            Assert.True(receivedDataRate);
        }

        [Fact]
        public void SamplingRateTest()
        {
            InitServiceHelper();

            DataRateCalculator samplingRateCalculator = new DataRateCalculator(100);
            samplingRateCalculator.Start();

            double nominalDataPerSecond = 192;
            double nominalSamplingDataRatePerSecond = 10;
            TimeSpan duration = TimeSpan.FromSeconds(100);
            WriteData(100, nominalDataPerSecond, duration, nominalSamplingDataRatePerSecond, (datum, calculatedDataPerSecond, action) =>
            {                
                if (action == DataRateCalculator.SamplingAction.Keep)
                {
                    Assert.Equal(samplingRateCalculator.Add(datum), DataRateCalculator.SamplingAction.Keep);
                }
            });

            Assert.True(Math.Abs(samplingRateCalculator.GetDataPerSecond().Value - nominalSamplingDataRatePerSecond) <= 1);
        }

        [Fact]
        public void DropNullDatumTest()
        {
            DataRateCalculator samplingRateCalculator = new DataRateCalculator(100);
            samplingRateCalculator.Start();
            Assert.Equal(samplingRateCalculator.Add(null), DataRateCalculator.SamplingAction.Drop);
        }

        [Fact]
        public void ImmediateDataTest()
        {
            InitServiceHelper();

            DataRateCalculator calculator = new DataRateCalculator(10, 1);
            AccelerometerDatum datum = new AccelerometerDatum(DateTimeOffset.UtcNow, 1, 1, 1);
            calculator.Start(datum.Timestamp);
            for (int i = 0; i < calculator.SampleSize * 2; ++i)
            {
                DataRateCalculator.SamplingAction action = calculator.Add(datum);

                if (i < calculator.SampleSize)
                {
                    Assert.Equal(action, DataRateCalculator.SamplingAction.Keep);
                }
                else
                {
                    Assert.Equal(action, DataRateCalculator.SamplingAction.Drop);
                }
            }
        }

        private void WriteData(long sampleSize, double dataPerSecond, TimeSpan duration, double? maxSamplesToKeepPerSecond, Action<Datum, double, DataRateCalculator.SamplingAction> calculatedDataRateKeepCallback)
        {
            DataRateCalculator dataRateCalculator = new DataRateCalculator(sampleSize, maxSamplesToKeepPerSecond);

            long numData = (long)(dataPerSecond * duration.TotalSeconds);
            double interDataTime = duration.TotalSeconds / numData;
            DateTimeOffset startTimestamp = DateTimeOffset.UtcNow;
            dataRateCalculator.Start(startTimestamp);
            for (long i = 0; i < numData; ++i)
            {
                DateTimeOffset simulatedCurrentTime = startTimestamp.AddSeconds((i + 1) * interDataTime);
                AccelerometerDatum datum = new AccelerometerDatum(simulatedCurrentTime, 1, 1, 1);
                DataRateCalculator.SamplingAction samplingAction = dataRateCalculator.Add(datum);

                double? calculatedDataPerSecond = dataRateCalculator.GetDataPerSecond();

                if (i < sampleSize - 1)
                {
                    Assert.Null(calculatedDataPerSecond);
                }
                else
                {
                    Assert.NotNull(calculatedDataPerSecond);
                    calculatedDataRateKeepCallback?.Invoke(datum, calculatedDataPerSecond.Value, samplingAction);
                }
            }
        }

        private void InitServiceHelper()
        {
            SensusServiceHelper.ClearSingleton();
            TestSensusServiceHelper serviceHelper = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => serviceHelper);
        }
    }
}
