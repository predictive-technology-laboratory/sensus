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
