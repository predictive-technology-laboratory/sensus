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
using Android.Content;
using Android.Gms.Awareness.Fence;
using Sensus.Exceptions;
using Sensus.Probes.Movement;

namespace Sensus.Android.Probes.Movement
{
    /// <summary>
    /// Broadcast receiver for all Android Awareness API events.
    /// </summary>
    public class AndroidAwarenessProbeBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler<ActivityDatum> ActivityChanged;
        public event EventHandler<FenceState> LocationChanged;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                if (intent.Action == AndroidAwarenessProbe.AWARENESS_PENDING_INTENT_ACTION)
                {
                    FenceState fenceState = FenceState.Extract(intent);
                    if (fenceState == null)
                    {
                        SensusException.Report("Null awareness fence state.");
                        return;
                    }

                    // is this a location fence event?
                    if (fenceState.FenceKey == AndroidLocationFenceProbe.AWARENESS_EXITING_LOCATION_FENCE_KEY)
                    {
                        if (fenceState.CurrentState == FenceState.True)
                        {
                            LocationChanged?.Invoke(this, fenceState);
                        }
                    }
                    // otherwise, it should be an activity fence event.
                    else
                    {
                        string[] fenceKeyParts;
                        if (fenceState.FenceKey == null || (fenceKeyParts = fenceState.FenceKey.Split('.')).Length != 2)
                        {
                            SensusException.Report("Awareness fence key \"" + fenceState.FenceKey + "\" does not have two parts.");
                            return;
                        }

                        Activities activity;
                        if (!Enum.TryParse(fenceKeyParts[0], out activity))
                        {
                            SensusException.Report("Unrecognized awareness activity:  " + fenceKeyParts[0]);
                            return;
                        }

                        ActivityPhase phase;
                        if (!Enum.TryParse(fenceKeyParts[1], out phase))
                        {
                            SensusException.Report("Unrecognized awareness activity phase:  " + fenceKeyParts[1]);
                            return;
                        }

                        DateTimeOffset timestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(fenceState.LastFenceUpdateTimeMillis);

                        if (fenceState.CurrentState == FenceState.True)
                        {
                            ActivityChanged?.Invoke(this, new ActivityDatum(timestamp, activity, phase, ActivityState.Active, ActivityConfidence.NotAvailable));
                        }
                        else if (fenceState.CurrentState == FenceState.False)
                        {
                            ActivityChanged?.Invoke(this, new ActivityDatum(timestamp, activity, phase, ActivityState.Inactive, ActivityConfidence.NotAvailable));
                        }
                        else if (fenceState.CurrentState == FenceState.Unknown)
                        {
                            ActivityChanged?.Invoke(this, new ActivityDatum(timestamp, activity, phase, ActivityState.Unknown, ActivityConfidence.NotAvailable));
                        }
                        else
                        {
                            SensusException.Report("Unrecognized fence state:  " + fenceState.CurrentState);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                SensusException.Report("Exception in Awareness broadcast receiver:  " + ex.Message, ex);
            }
        }
    }
}