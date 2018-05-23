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
using NUnit.Framework;
using Sensus.Probes;
using Sensus.Probes.Movement;
using Sensus.Tests.Classes;

namespace Sensus.Tests.Sensus.Shared.Probes
{
    [TestFixture]
    public class DataRateCalculatorTests
    {
        [Test]
        public void SampleSizeTest()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), new TestDelegate(() => new DataRateCalculator(-1)));
            Assert.Throws(typeof(ArgumentOutOfRangeException), new TestDelegate(() => new DataRateCalculator(0)));
            Assert.Throws(typeof(ArgumentOutOfRangeException), new TestDelegate(() => new DataRateCalculator(1)));
            Assert.DoesNotThrow(new TestDelegate(() => new DataRateCalculator(2)));
            Assert.DoesNotThrow(new TestDelegate(() => new DataRateCalculator(100)));
        }

        [Test]
        public void DataRateTest1()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 10;
            WriteData(2, nominalDataPerSecond, TimeSpan.FromSeconds(1000), calculatedDataPerSecond =>
            {
                Assert.AreEqual(calculatedDataPerSecond, nominalDataPerSecond);
            });
        }

        [Test]
        public void DataRateTest2()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 13;
            WriteData(12, nominalDataPerSecond, TimeSpan.FromSeconds(1000), calculatedDataPerSecond =>
            {
                Assert.LessOrEqual(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond), 1);
            });
        }

        [Test]
        public void DataRateTest3()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 13;
            WriteData(14, nominalDataPerSecond, TimeSpan.FromSeconds(1000), calculatedDataPerSecond =>
            {
                Assert.LessOrEqual(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond), 1);
            });
        }

        [Test]
        public void DataRateTest4()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 47;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(1000), calculatedDataPerSecond =>
            {
                Assert.LessOrEqual(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond), 1);
            });
        }

        [Test]
        public void DataRateTest5()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 113;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(1000), calculatedDataPerSecond =>
            {
                Assert.LessOrEqual(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond), 1);
            });
        }

        [Test]
        public void DataRateTest6()
        {
            InitServiceHelper();

            double nominalDataPerSecond = 192;
            WriteData(100, nominalDataPerSecond, TimeSpan.FromSeconds(1000), calculatedDataPerSecond =>
            {
                Assert.LessOrEqual(Math.Abs(calculatedDataPerSecond - nominalDataPerSecond), 1);
            });
        }

        private void WriteData(long sampleSize, double dataPerSecond, TimeSpan duration, Action<double> calculatedDataRateCallback)
        {
            DataRateCalculator dataRateCalculator = new DataRateCalculator(sampleSize);

            long numData = (long)(dataPerSecond * duration.TotalSeconds);
            double interDataTime = duration.TotalSeconds / numData;
            DateTimeOffset startTimestamp = DateTimeOffset.UtcNow;
            dataRateCalculator.Start(startTimestamp);
            for (long i = 0; i < numData; ++i)
            {
                DateTimeOffset simulatedCurrentTime = startTimestamp.AddSeconds((i + 1) * interDataTime);
                dataRateCalculator.Add(new AccelerometerDatum(simulatedCurrentTime, 1, 1, 1));

                double? calculatedDataPerSecond = dataRateCalculator.DataPerSecond;

                if (i < sampleSize - 1)
                {
                    Assert.IsNull(calculatedDataPerSecond);
                }
                else
                {
                    Assert.NotNull(calculatedDataPerSecond);
                    calculatedDataRateCallback?.Invoke(calculatedDataPerSecond.Value);
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
