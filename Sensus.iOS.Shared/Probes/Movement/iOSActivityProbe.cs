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
using System.Threading;
using CoreMotion;
using Foundation;
using Newtonsoft.Json;
using Sensus.Context;
using Sensus.Exceptions;
using Sensus.Probes;
using Sensus.Probes.Movement;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms.Platform.iOS;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.Movement
{
    /// <summary>
    /// Provides inferred activity information via the iOS activity recognition API as <see cref="ActivityDatum"/> readings.
    /// </summary>
    public class iOSActivityProbe : PollingProbe
    {
        private DateTimeOffset? _queryStartTime;

        public DateTimeOffset? QueryStartTime
        {
            get { return _queryStartTime; }
            set { _queryStartTime = value; }
        }

        [JsonIgnore]
        public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(3).TotalMilliseconds;

        [JsonIgnore]
        public override string DisplayName => "Activity";

        [JsonIgnore]
        public override Type DatumType => typeof(ActivityDatum);

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        public iOSActivityProbe()
        {
            _queryStartTime = null;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!CMMotionActivityManager.IsActivityAvailable)
            {
                throw new NotSupportedException("Activity data are not available on this device.");
            }
        }

        protected override async Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            List<Datum> data = new List<Datum>();

            // if this is the first poll (no existing query start time), set the query start time to the current time. we
            // used to set this to the maximally previous time per ios documentation (7 days), but this (1) causes issues
            // when triggering surveys on the basis of these activities (there might be hundreds of activities within the
            // past 7 days), and it also runs counter to the user's expectations that data will only be collected from the
            // time at which they have enrolled in the study and not from times prior.
            if (_queryStartTime == null)
            {
                _queryStartTime = DateTimeOffset.UtcNow;
            }

            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                try
                {
                    CMMotionActivityManager activityManager = new CMMotionActivityManager();

                    CMMotionActivity[] activities = await activityManager.QueryActivityAsync(_queryStartTime.Value.UtcDateTime.ToNSDate(), NSDate.Now, NSOperationQueue.CurrentQueue);

                    // process each activity, keeping track of most recent
                    NSDate mostRecentActivityStartTime = null;
                    foreach (CMMotionActivity activity in activities)
                    {
                        DateTimeOffset timestamp = new DateTimeOffset(activity.StartDate.ToDateTime(), TimeSpan.Zero);

                        #region get confidence
                        ActivityConfidence confidence = ActivityConfidence.NotAvailable;

                        if (activity.Confidence == CMMotionActivityConfidence.Low)
                        {
                            confidence = ActivityConfidence.Low;
                        }
                        else if (activity.Confidence == CMMotionActivityConfidence.Medium)
                        {
                            confidence = ActivityConfidence.Medium;
                        }
                        else if (activity.Confidence == CMMotionActivityConfidence.High)
                        {
                            confidence = ActivityConfidence.High;
                        }
                        else
                        {
                            SensusException.Report("Unrecognized confidence:  " + activity.Confidence);
                        }
                        #endregion

                        #region get activities
                        Action<Activities> AddActivityDatum = activityType =>
                        {
                            ActivityDatum activityDatum = new ActivityDatum(timestamp, activityType, ActivityPhase.Starting, ActivityState.Active, confidence);
                            data.Add(activityDatum);
                        };

                        if (activity.Stationary)
                        {
                            AddActivityDatum(Activities.Still);
                        }

                        if (activity.Walking)
                        {
                            AddActivityDatum(Activities.Walking);
                        }

                        if (activity.Running)
                        {
                            AddActivityDatum(Activities.Running);
                        }

                        if (activity.Automotive)
                        {
                            AddActivityDatum(Activities.InVehicle);
                        }

                        if (activity.Cycling)
                        {
                            AddActivityDatum(Activities.OnBicycle);
                        }

                        if (activity.Unknown)
                        {
                            AddActivityDatum(Activities.Unknown);
                        }
                        #endregion

                        if (mostRecentActivityStartTime == null)
                        {
                            mostRecentActivityStartTime = activity.StartDate;
                        }
                        else
                        {
                            mostRecentActivityStartTime = mostRecentActivityStartTime.LaterDate(activity.StartDate);
                        }
                    }

                    // set the next query start time one second after the most recent activity's start time
                    if (mostRecentActivityStartTime != null)
                    {
                        _queryStartTime = new DateTime(mostRecentActivityStartTime.ToDateTime().Ticks, DateTimeKind.Utc).AddSeconds(1);
                    }
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while querying activities:  " + ex, LoggingLevel.Normal, GetType());
                }
            });

            // let the system know that we polled but didn't get any data
            if (data.Count == 0)
            {
                data.Add(null);
            }

            return data;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }
    }
}
