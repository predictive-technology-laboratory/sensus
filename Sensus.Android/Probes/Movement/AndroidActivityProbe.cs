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
using Android.Gms.Awareness.Fence;
using Sensus.Exceptions;
using Sensus.Probes.Movement;
using Newtonsoft.Json;

namespace Sensus.Android.Probes.Movement
{
    /// <summary>
    /// Provides inferred activity information via the Google Awareness API as <see cref="ActivityDatum"/> readings.
    /// </summary>
    public class AndroidActivityProbe : AndroidAwarenessProbe
    {
        [JsonIgnore]
        public override Type DatumType
        {
            get
            {
                return typeof(ActivityDatum);
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get
            {
                return "Activity";
            }
        }

        public AndroidActivityProbe()
        {
            AwarenessBroadcastReceiver.ActivityChanged += (sender, activityDatum) =>
            {
                StoreDatumAsync(activityDatum);
            };
        }

        protected override void StartListening()
        {
            base.StartListening();

            // add fences for all phases of all activities
            AddPhasedFences(DetectedActivityFence.InVehicle, nameof(DetectedActivityFence.InVehicle));
            AddPhasedFences(DetectedActivityFence.OnBicycle, nameof(DetectedActivityFence.OnBicycle));
            AddPhasedFences(DetectedActivityFence.OnFoot, nameof(DetectedActivityFence.OnFoot));
            AddPhasedFences(DetectedActivityFence.Running, nameof(DetectedActivityFence.Running));
            AddPhasedFences(DetectedActivityFence.Still, nameof(DetectedActivityFence.Still));
            AddPhasedFences(DetectedActivityFence.Unknown, nameof(DetectedActivityFence.Unknown));
            AddPhasedFences(DetectedActivityFence.Walking, nameof(DetectedActivityFence.Walking));
        }

        private void AddPhasedFences(int activityId, string activityName)
        {
            FenceUpdateRequestBuilder addFencesRequestBuilder = new FenceUpdateRequestBuilder();
            UpdateRequestBuilder(activityId, activityName, ActivityPhase.Starting, FenceUpdateAction.Add, ref addFencesRequestBuilder);
            UpdateRequestBuilder(activityId, activityName, ActivityPhase.During, FenceUpdateAction.Add, ref addFencesRequestBuilder);
            UpdateRequestBuilder(activityId, activityName, ActivityPhase.Stopping, FenceUpdateAction.Add, ref addFencesRequestBuilder);
            UpdateFences(addFencesRequestBuilder.Build());
        }

        private void UpdateRequestBuilder(int activityId, string activityName, ActivityPhase phase, FenceUpdateAction action, ref FenceUpdateRequestBuilder requestBuilder)
        {
            AwarenessFence fence = null;

            if (phase == ActivityPhase.Starting)
            {
                fence = DetectedActivityFence.Starting(activityId);
            }
            else if (phase == ActivityPhase.During)
            {
                fence = DetectedActivityFence.During(activityId);
            }
            else if (phase == ActivityPhase.Stopping)
            {
                fence = DetectedActivityFence.Stopping(activityId);
            }
            else
            {
                SensusException.Report("Unknown activity phase:  " + phase);
                return;
            }

            UpdateRequestBuilder(fence, activityName + "." + phase, action, ref requestBuilder);
        }

        protected override void StopListening()
        {
            RemovePhasedFences(DetectedActivityFence.InVehicle, nameof(DetectedActivityFence.InVehicle));
            RemovePhasedFences(DetectedActivityFence.OnBicycle, nameof(DetectedActivityFence.OnBicycle));
            RemovePhasedFences(DetectedActivityFence.OnFoot, nameof(DetectedActivityFence.OnFoot));
            RemovePhasedFences(DetectedActivityFence.Running, nameof(DetectedActivityFence.Running));
            RemovePhasedFences(DetectedActivityFence.Still, nameof(DetectedActivityFence.Still));
            RemovePhasedFences(DetectedActivityFence.Unknown, nameof(DetectedActivityFence.Unknown));
            RemovePhasedFences(DetectedActivityFence.Walking, nameof(DetectedActivityFence.Walking));

            base.StopListening();
        }

        private void RemovePhasedFences(int activityId, string activityName)
        {
            try
            {
                FenceUpdateRequestBuilder removeFencesRequestBuilder = new FenceUpdateRequestBuilder();
                UpdateRequestBuilder(activityId, activityName, ActivityPhase.Starting, FenceUpdateAction.Remove, ref removeFencesRequestBuilder);
                UpdateRequestBuilder(activityId, activityName, ActivityPhase.During, FenceUpdateAction.Remove, ref removeFencesRequestBuilder);
                UpdateRequestBuilder(activityId, activityName, ActivityPhase.Stopping, FenceUpdateAction.Remove, ref removeFencesRequestBuilder);

                if (!UpdateFences(removeFencesRequestBuilder.Build()))
                {
                    // we'll catch this immediately
                    throw new Exception("Failed to remove fence (e.g., timed out).");
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing fence:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}