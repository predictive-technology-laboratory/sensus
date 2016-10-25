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
using System.Threading;
using System.Collections.Generic;
using Android.Media;
using Sensus.Shared;
using Sensus.Shared.Probes.Context;
using Plugin.Permissions.Abstractions;

namespace Sensus.Shared.Android.Probes.Context
{
    public class AndroidSoundProbe : SoundProbe
    {
        protected override IEnumerable<Datum> Poll(CancellationToken cancellationToken)
        {
            MediaRecorder recorder = null;
            try
            {
                if (SensusServiceHelper.Get().ObtainPermission(Permission.Microphone) != PermissionStatus.Granted)
                    throw new Exception("Cannot access microphone.");

                recorder = new MediaRecorder();
                recorder.SetAudioSource(AudioSource.Mic);
                recorder.SetOutputFormat(OutputFormat.ThreeGpp);
                recorder.SetAudioEncoder(AudioEncoder.AmrNb);
                recorder.SetOutputFile("/dev/null");
                recorder.Prepare();
                recorder.Start();

                // mark start time of amplitude measurement -- MaxAmplitude is always computed from previous call to MaxAmplitude
                int dummy = recorder.MaxAmplitude;

                Thread.Sleep(SampleLengthMS);

                return new Datum[] { new SoundDatum(DateTimeOffset.UtcNow, 20 * Math.Log10(recorder.MaxAmplitude)) };  // http://www.mathworks.com/help/signal/ref/mag2db.html
            }
            finally
            {
                if (recorder != null)
                {
                    try { recorder.Stop(); }
                    catch (Exception) { }

                    try { recorder.Release(); }
                    catch (Exception) { }
                }
            }
        }
    }
}
