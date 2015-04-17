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
using SensusService.Probes.Context;
using System.Collections.Generic;
using AVFoundation;
using Foundation;
using System.IO;
using SensusService;
using System.Threading;

namespace Sensus.iOS.Probes.Context
{
    /// <summary>
    /// Probes sound level (decibels) from microphone. http://developer.xamarin.com/recipes/ios/media/sound/record_sound
    /// </summary>
    public class iOSSoundProbe : SoundProbe
    {
        NSDictionary _settings;

        protected override void Initialize()
        {
            base.Initialize();

            NSObject[] settingsKeys = new NSObject[]
            {
                AVAudioSettings.AVSampleRateKey,
                AVAudioSettings.AVFormatIDKey,
                AVAudioSettings.AVNumberOfChannelsKey,
                AVAudioSettings.AVLinearPCMBitDepthKey,
                AVAudioSettings.AVLinearPCMIsBigEndianKey,
                AVAudioSettings.AVLinearPCMIsFloatKey
            };
            
            NSObject[] settingsValues = new NSObject[]
            {
                NSNumber.FromFloat(44100.0f),
                NSNumber.FromInt32((int)AudioToolbox.AudioFormatType.LinearPCM),
                NSNumber.FromInt32(2),
                NSNumber.FromInt32(16),
                NSNumber.FromBoolean(false),
                NSNumber.FromBoolean(false)
            };

            _settings = NSDictionary.FromObjectsAndKeys(settingsValues, settingsKeys);
        }

        protected override IEnumerable<Datum> Poll(System.Threading.CancellationToken cancellationToken)
        {            
            float decibels = float.NaN;

            AVAudioRecorder recorder = null;
            ManualResetEvent recorderWait = new ManualResetEvent(false);
            try
            {
                AVAudioSession audioSession = AVAudioSession.SharedInstance();

                audioSession.RequestRecordPermission(granted => SensusServiceHelper.Get().Logger.Log("Audio record permission granted:  " + granted, LoggingLevel.Verbose, GetType()));

                NSError error = audioSession.SetCategory(AVAudioSessionCategory.Record);
                if (error != null)
                    throw new Exception("Failed to initialize iOS audio session:  " + error.LocalizedDescription);

                error = audioSession.SetActive(true);
                if (error != null)
                    throw new Exception("Failed to make audio session active:  " + error.LocalizedDescription);
                
                recorder = AVAudioRecorder.Create(NSUrl.FromFilename("/dev/null"), new AudioSettings(_settings), out error);
                if (error != null)
                    throw new Exception("Failed to create sound recorder:  " + error.LocalizedDescription);

                recorder.FinishedRecording += (o,e) => { recorderWait.Set(); };
                recorder.MeteringEnabled = true;

                if (recorder.RecordFor(SampleLengthMS / 1000d))
                {
                    recorderWait.WaitOne();
                    recorder.UpdateMeters();
                    decibels = recorder.AveragePower(0) + 160; // range looks to be [-160 - 0] from http://b2cloud.com.au/tutorial/obtaining-decibels-from-the-ios-microphone
    
                    return new Datum[] { new SoundDatum(DateTimeOffset.UtcNow, decibels) };
                }
                else
                    throw new Exception("Failed to record.");
            }
            catch (Exception)
            {
                return new Datum[] { };
            }
            finally
            {
                if (recorder != null)
                {
                    try
                    {
                        recorder.Stop();
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        recorder.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }                
        }
    }
}