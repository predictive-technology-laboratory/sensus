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
using Sensus.Probes.Context;
using System.Collections.Generic;
using AVFoundation;
using Foundation;
using System.IO;
using Sensus;
using System.Threading;

using System.Threading.Tasks;
using System.Linq;

namespace Sensus.iOS.Probes.Context
{
    /// <summary>
    /// Probes sound level (decibels) from microphone. http://developer.xamarin.com/recipes/ios/media/sound/record_sound
    /// </summary>
    public class iOSSoundProbe : SoundProbe
    {
        private NSDictionary _settings;

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            NSObject[] settingsKeys = new NSObject[]
            {
                AVAudioSettings.AVSampleRateKey,
                AVAudioSettings.AVFormatIDKey
            };
            
            NSObject[] settingsValues = new NSObject[]
            {
                NSNumber.FromFloat(16000.0f),
                NSNumber.FromInt32((int)AudioToolbox.AudioFormatType.LinearPCM),
            };

            _settings = NSDictionary.FromObjectsAndKeys(settingsValues, settingsKeys);
        }

        protected override async Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
        {
            AVAudioRecorder recorder = null;
            string recordPath = Path.GetTempFileName();
            try
            {
                AVAudioSession audioSession = AVAudioSession.SharedInstance();

                NSError error = audioSession.SetCategory(AVAudioSessionCategory.Record);
                if (error != null)
                {
                    throw new Exception("Failed to initialize iOS audio recording session:  " + error.LocalizedDescription);
                }

                error = audioSession.SetActive(true);
                if (error != null)
                {
                    throw new Exception("Failed to make audio session active:  " + error.LocalizedDescription);
                }

                recorder = AVAudioRecorder.Create(NSUrl.FromFilename(recordPath), new AudioSettings(_settings), out error);
                if (error != null)
                {
                    throw new Exception("Failed to create sound recorder:  " + error.LocalizedDescription);
                }

                recorder.MeteringEnabled = true;

                // we need to take a meter reading while the recorder is running, so record for one second beyond the sample length
                if (recorder.RecordFor(SampleLengthMS / 1000d + 1))
                {
                    await Task.Delay(SampleLengthMS);
                    recorder.UpdateMeters();
                    double decibels = 100 * (recorder.PeakPower(0) + 160) / 160f;  // range looks to be [-160 - 0] from http://b2cloud.com.au/tutorial/obtaining-decibels-from-the-ios-microphone

                    return new Datum[] { new SoundDatum(DateTimeOffset.UtcNow, decibels) }.ToList();
                }
                else
                {
                    throw new Exception("Failed to start recording.");
                }
            }
            finally
            {
                try
                {
                    File.Delete(recordPath);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to delete sound file:  " + ex.Message, LoggingLevel.Debug, GetType());
                }

                if (recorder != null)
                {
                    try
                    {
                        recorder.Stop();
                    }
                    catch (Exception) { }

                    try
                    {
                        recorder.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}