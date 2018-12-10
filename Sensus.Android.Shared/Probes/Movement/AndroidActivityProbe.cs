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
using Android.Gms.Awareness.Fence;
using Sensus.Exceptions;
using Sensus.Probes.Movement;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Movement
{
    /// <summary>
    /// Provides inferred activity information via the Google Awareness API as <see cref="ActivityDatum"/> readings. As it is not
    /// possible to do continuous, real time activity recognition due to battery and network limitations, there are several caveats
    /// to the data produced by this probe:
    /// 
    ///   * The user must typically be engaged in the activity for 30 seconds or more for the activity to be detected.
    ///   * Activities are typically reported when the user unlocks the device. They are rarely reported when the device is locked.
    ///   * Activities have both a <see cref="ActivityPhase"/> and <see cref="ActivityState"/>, which must be carefully
    ///     inspected to understand the activity the user is engaged in.
    /// 
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
            AwarenessBroadcastReceiver.ActivityChanged += async (sender, activityDatum) =>
            {
                await StoreDatumAsync(activityDatum);
            };
        }

        protected override async Task StartListeningAsync()
        {
            await base.StartListeningAsync();

            // add fences for all phases of all activities
            await AddPhasedFencesAsync(DetectedActivityFence.InVehicle, nameof(DetectedActivityFence.InVehicle));
            await AddPhasedFencesAsync(DetectedActivityFence.OnBicycle, nameof(DetectedActivityFence.OnBicycle));
            await AddPhasedFencesAsync(DetectedActivityFence.OnFoot, nameof(DetectedActivityFence.OnFoot));
            await AddPhasedFencesAsync(DetectedActivityFence.Running, nameof(DetectedActivityFence.Running));
            await AddPhasedFencesAsync(DetectedActivityFence.Still, nameof(DetectedActivityFence.Still));
            await AddPhasedFencesAsync(DetectedActivityFence.Unknown, nameof(DetectedActivityFence.Unknown));
            await AddPhasedFencesAsync(DetectedActivityFence.Walking, nameof(DetectedActivityFence.Walking));
        }

        private async Task AddPhasedFencesAsync(int activityId, string activityName)
        {
            FenceUpdateRequestBuilder addFencesRequestBuilder = new FenceUpdateRequestBuilder();
            UpdateRequestBuilder(activityId, activityName, ActivityPhase.Starting, FenceUpdateAction.Add, ref addFencesRequestBuilder);
            UpdateRequestBuilder(activityId, activityName, ActivityPhase.During, FenceUpdateAction.Add, ref addFencesRequestBuilder);
            UpdateRequestBuilder(activityId, activityName, ActivityPhase.Stopping, FenceUpdateAction.Add, ref addFencesRequestBuilder);
            await UpdateFencesAsync(addFencesRequestBuilder.Build());
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

        protected override async Task StopListeningAsync()
        {
            await RemovePhasedFencesAsync(DetectedActivityFence.InVehicle, nameof(DetectedActivityFence.InVehicle));
            await RemovePhasedFencesAsync(DetectedActivityFence.OnBicycle, nameof(DetectedActivityFence.OnBicycle));
            await RemovePhasedFencesAsync(DetectedActivityFence.OnFoot, nameof(DetectedActivityFence.OnFoot));
            await RemovePhasedFencesAsync(DetectedActivityFence.Running, nameof(DetectedActivityFence.Running));
            await RemovePhasedFencesAsync(DetectedActivityFence.Still, nameof(DetectedActivityFence.Still));
            await RemovePhasedFencesAsync(DetectedActivityFence.Unknown, nameof(DetectedActivityFence.Unknown));
            await RemovePhasedFencesAsync(DetectedActivityFence.Walking, nameof(DetectedActivityFence.Walking));

            await base.StopListeningAsync();
        }

        private async Task RemovePhasedFencesAsync(int activityId, string activityName)
        {
            try
            {
                FenceUpdateRequestBuilder removeFencesRequestBuilder = new FenceUpdateRequestBuilder();
                UpdateRequestBuilder(activityId, activityName, ActivityPhase.Starting, FenceUpdateAction.Remove, ref removeFencesRequestBuilder);
                UpdateRequestBuilder(activityId, activityName, ActivityPhase.During, FenceUpdateAction.Remove, ref removeFencesRequestBuilder);
                UpdateRequestBuilder(activityId, activityName, ActivityPhase.Stopping, FenceUpdateAction.Remove, ref removeFencesRequestBuilder);

                if (!(await UpdateFencesAsync(removeFencesRequestBuilder.Build())))
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
