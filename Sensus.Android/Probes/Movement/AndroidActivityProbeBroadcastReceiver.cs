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
    public class AndroidActivityProbeBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler<ActivityDatum> ActivityChanged;
        public event EventHandler<FenceState> LocationChanged;

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            if (intent.Action.StartsWith(AndroidActivityProbe.ACTION_BASE))
            {
                FenceState fenceState = FenceState.Extract(intent);

                if (fenceState.FenceKey.StartsWith(AndroidActivityProbe.ACTION_BASE))
                {
                    string awarenessAction = fenceState.FenceKey.Substring(fenceState.FenceKey.IndexOf(".") + 1);

                    if (awarenessAction == AndroidActivityProbe.ACTIVITY_RECOGNITION_LOCATION)
                    {
                        LocationChanged?.Invoke(this, fenceState);
                    }
                    else
                    {
                        DateTimeOffset timestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan()).AddMilliseconds(fenceState.LastFenceUpdateTimeMillis);
                        Activities activity = (Activities)Enum.Parse(typeof(Activities), awarenessAction);

                        if (fenceState.CurrentState == FenceState.True)
                        {
                            ActivityChanged?.Invoke(this, new ActivityDatum(timestamp, activity, ActivityState.Active));
                        }
                        else if (fenceState.CurrentState == FenceState.False)
                        {
                            ActivityChanged?.Invoke(this, new ActivityDatum(timestamp, activity, ActivityState.Inactive));
                        }
                        else if (fenceState.CurrentState == FenceState.Unknown)
                        {
                            ActivityChanged?.Invoke(this, new ActivityDatum(timestamp, activity, ActivityState.Unknown));
                        }
                        else
                        {
                            SensusException.Report("Unrecognized fence state:  " + fenceState.CurrentState);
                        }
                    }
                }
            }
        }
    }
}