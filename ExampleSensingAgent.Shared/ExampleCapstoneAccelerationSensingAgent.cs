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
    ///     (1) determine on/off sensor settings
    ///     (2) test
    /// </remarks>
    public class ExampleCapstoneAccelerationSensingAgent : SensingAgent
    {
        #region Private Classes
        /// <summary>
        /// A helper class for calculating, storing and passing model features.
        /// </summary>
        private class Features
        {
            #region Public Properties
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
            #endregion

            #region Constructor
            public Features(DateTime listeningWindowStart, IEnumerable<IAccelerometerDatum> accellerationData)
            {
                Hour = listeningWindowStart.Hour;
                Day  = ((int)listeningWindowStart.DayOfWeek + 6) % 7;

                Acceleration = new Statistics(accellerationData.Select(ToMagnitude));
            }
            #endregion

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
        /// <summary>
        /// Time in which the sensors plus model determine if sensors should be on or off in prediction window
        /// </summary>
        private TimeSpan ListeningWindow
        {
            get
            {
                return ActiveObservationDuration.Value;
            }

            set
            {
                ActiveObservationDuration = value;
                ActionInterval = value + PredictingWindow;
            }
        }

        /// <summary>
        /// Time during which the sensors are controlled by this class based on the results of the listening window and model
        /// </summary>
        private TimeSpan PredictingWindow 
        {
            get
            {
                
                return ControlCompletionCheckInterval;
            }

            set
            {
                ControlCompletionCheckInterval = value;
                ActionInterval = ListeningWindow + value;
            }
        }

        /// <summary>
        /// Features for the listening window one cycle ago (i.e., the previous cycle)
        /// </summary>
        private Features FeaturesLag1 { get; set; }
        
        /// <summary>
        /// Features for the listening window two cycles ago
        /// </summary>
        private Features FeaturesLag2 { get; set; }
        
        /// <summary>
        /// Features for the listening window three cycles ago
        /// </summary>
        private Features FeaturesLag3 { get; set; }
        #endregion

        #region Constructors
        public ExampleCapstoneAccelerationSensingAgent(): base("Capstone-2020", "ALM/TOD", default, default, default)
        {
            ListeningWindow  = TimeSpan.FromSeconds(10);
            PredictingWindow = TimeSpan.FromSeconds(60);
        }
        #endregion

        #region Protected Overrides Methods
        protected override Task ProtectedSetPolicyAsync(JObject policy)
        {
            ListeningWindow  = TimeSpan.FromSeconds(double.Parse(policy["cps-listening"].ToString()));
            PredictingWindow = TimeSpan.FromSeconds(double.Parse(policy["cps-predicting"].ToString()));

            return Task.CompletedTask;
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            //collect sensor data for making decisions
            var accelerometerData    = GetObservedData<IAccelerometerDatum>().Cast<IAccelerometerDatum>();
            var listeningWindowStart = DateTime.Now.Subtract(ListeningWindow);

            //calculate and store features for model decision
            var featuresLag0 = new Features(listeningWindowStart, accelerometerData);
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
                // determine the appropriate sensor settings for an "on" prediction window
            }
            else if (currentState == SensingAgentState.EndingControl)
            {
                // determine the appropriate sensor settings for an "off" prediction window
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
            return features.Where(f => f != null).Count() < 2;
        }
        #endregion
    }
}