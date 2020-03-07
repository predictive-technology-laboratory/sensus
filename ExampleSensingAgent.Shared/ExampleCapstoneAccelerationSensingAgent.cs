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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using Sensus;
using Sensus.Probes.Movement;
using Sensus.Probes;
using System.Threading;
using Sensus.Extensions;
using Sensus.Adaptation;

namespace ExampleSensingAgent
{
    /// <summary>
    /// The sekeleton implementation for the capstone 2020 active sensing project
    /// </summary>
    /// <remarks>
    /// TODO:
    ///     (1) figure out how to get time stamp for start of listening window
    ///     (2) figure out appropriate settings for active sensing
    ///     (3) implement appropriate set policy options
    ///     (4) determine appropriate values for base constructor call
    ///     (5) test
    /// </remarks>
    public class ExampleCapstoneAccelerationSensingAgent : SensingAgent
    {
        #region Private Classes
        /// <summary>
        /// A helper class for calculating, storing and passing model features.
        /// </summary>
        private class Features
        {
            /// <summary>
            /// The hour in 24 hour time (i.e. 0-23) at the start of the listening window
            /// </summary>
            public int Hour { get; private set; }

            /// <summary>
            /// The day of week index at the start of the listening window with Monday == 0
            /// </summary>
            public int Day { get; private set; }

            /// <summary>
            /// True if Day is Saturday or Sunday at the start of the listening window
            /// </summary>
            public bool IsWeekend { get { return Day == 5 || Day == 6; } }

            /// <summary>
            /// The summary statistics (i.e., mean, median, SD and Range) for the accelerometer data
            /// </summary>
            public Statistics Acceleration { get; private set; }
            
            public Features(DateTime windowStart, IEnumerable<IAccelerometerDatum> accellerationData)
            {
                Hour = windowStart.Hour;
                Day  = ((int)windowStart.DayOfWeek + 6) % 7;

                Acceleration = new Statistics(accellerationData.Select(ToMagnitude));
            }

            #region Private Methods
            /// <summary>
            /// Convert the vector form of the accelerometer datums into their magnitude
            /// </summary>
            /// <param name="datum">an accelerometer datum representing a 3 dimensional vector</param>
            /// <returns>the magnitude of the given 3 dimensional acceleration datum </returns>
            private double ToMagnitude(IAccelerometerDatum datum)
            {
                return Math.Sqrt(Math.Pow(datum.X, 2) + Math.Pow(datum.Y, 2) + Math.Pow(datum.Z, 2));
            }
            #endregion
        }

        /// <summary>
        /// A helper class to make storing and referencing summary statistics easier
        /// </summary>
        private class Statistics
        {
            #region Public Properties
            public double Mean { get; private set; }
            public double Median { get; private set; }
            public double Range { get; private set; }
            public double Variance { get; private set; }
            public double StandardDeviation { get { return Math.Sqrt(Variance); } }
            #endregion

            #region Constructor
            public Statistics(IEnumerable<double> values)
            {
                // make sure the values are only 
                // materialized into memory once
                values = values.ToArray();

                Mean = CalculateMean(values);
                Median = CalculateMedian(values);
                Range = CalculateRange(values);
                Variance = CalculateVariance(values);
            }
            #endregion

            #region Private Methods
            private double CalculateMean(IEnumerable<double> values)
            {
                return values.Average();
            }

            private double CalculateMedian(IEnumerable<double> values)
            {
                //this could be made faster
                var ys = values.OrderBy(x => x).ToList();
                double mid = (ys.Count - 1) / 2.0;
                return (ys[(int)(mid)] + ys[(int)(mid + 0.5)]) / 2;
            }

            private double CalculateVariance(IEnumerable<double> values)
            {
                var avg = values.Average();

                return Math.Sqrt(values.Select(v => Math.Pow(v - avg, 2)).Average());
            }

            private double CalculateRange(IEnumerable<double> values)
            {
                return values.Max() - values.Min();
            }
            #endregion
        }
        #endregion

        #region Private Properties
        private Features FeaturesLag1 { get; set; }
        private Features FeaturesLag2 { get; set; }
        private Features FeaturesLag3 { get; set; }
        #endregion

        #region Constructors
        public ExampleCapstoneAccelerationSensingAgent()
            : base("Acceleration", "ALM / Proximity", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
            //model definition document says sensing will occur once a second (not sure how to use this)
            //model definition document defines an active phone as one with 3D-acceleration > 0.1 (not sure how to use this)

            //interval          = listening window + prediction window
            //listening window  = time in which the sensors plus model determine if sensors should be on or off in prediction window
            //prediction window = time in which the sensors are either turned on or off based on the listening window and model
        }
        #endregion

        #region Protected Overrides Methods
        protected override Task ProtectedSetPolicyAsync(JObject policy)
        {
            return Task.CompletedTask;
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            //collect sensor data for making decisions
            var accelerometerData = GetObservedData<IAccelerometerDatum>().Cast<IAccelerometerDatum>();

            //store features for model decision
            var featuresLag0 = new Features(DateTime.Now, accelerometerData);
            var featuresLag1 = FeaturesLag1;
            var featuresLag2 = FeaturesLag2;
            var featuresLag3 = FeaturesLag3;

            //bump feature lags up by 1
            FeaturesLag1 = featuresLag0;
            FeaturesLag2 = FeaturesLag1;
            FeaturesLag3 = FeaturesLag2;

            //use stored features to make control decision
            return ShouldControlPredictionWindow(new[] { featuresLag0, featuresLag1, featuresLag2, featuresLag3 });
        }

        protected override async Task OnStateChangedAsync(SensingAgentState previousState, SensingAgentState currentState, CancellationToken cancellationToken)
        {
            await base.OnStateChangedAsync(previousState, currentState, cancellationToken);

            if (currentState == SensingAgentState.OpportunisticControl || currentState == SensingAgentState.ActiveControl)
            {
                // keep device awake
                await SensusServiceHelper.KeepDeviceAwakeAsync();

                // increase sampling rate
                if (Protocol.TryGetProbe<IAccelerometerDatum, IListeningProbe>(out IListeningProbe accelerometerProbe))
                {
                    // increase sampling rate
                    accelerometerProbe.MaxDataStoresPerSecond = ControlAccelerometerMaxDataStoresPerSecond;

                    // restart probe to take on new settings
                    await accelerometerProbe.RestartAsync();
                }
            }
            else if (currentState == SensingAgentState.EndingControl)
            {
                if (Protocol.TryGetProbe<IAccelerometerDatum, IListeningProbe>(out IListeningProbe accelerometerProbe))
                {
                    // decrease sampling rate
                    accelerometerProbe.MaxDataStoresPerSecond = IdleAccelerometerMaxDataStoresPerSecond;

                    // restart probe to take on original settings
                    await accelerometerProbe.RestartAsync();
                }

                // let device sleep
                await SensusServiceHelper.LetDeviceSleepAsync();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// This function contains the decision rule used to take control of the sensors
        /// </summary>
        /// <param name="features">the current and three previous window features for model</param>
        /// <returns>true if we should take control of the sensors otherwise false</returns>
        private bool ShouldControlPredictionWindow(Features[] features)
        {            
            return false;
        }
        #endregion
    }
}