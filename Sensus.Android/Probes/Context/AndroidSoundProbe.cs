using Android.Media;
using SensusService;
using SensusService.Probes.Context;
using System;
using System.Collections.Generic;

namespace Sensus.Android.Probes.Context
{
    public class AndroidSoundProbe : SoundProbe
    {
        private MediaRecorder _recorder;

        protected override void Initialize()
        {
            base.Initialize();

            if (_recorder != null)
            {
                try { _recorder.Stop(); }  // will throw exception if recorder is already stopped
                catch (Exception) { }
            }

            _recorder = new MediaRecorder();

            _recorder.SetAudioSource(AudioSource.Mic);
            _recorder.SetOutputFormat(OutputFormat.ThreeGpp);
            _recorder.SetAudioEncoder(AudioEncoder.AmrNb);
            _recorder.SetOutputFile("/dev/null");
            _recorder.Prepare();
            _recorder.Start();
        }

        protected override IEnumerable<SensusService.Datum> Poll()
        {
            // http://www.mathworks.com/help/signal/ref/mag2db.html
            return new Datum[] { new SoundDatum(this, DateTimeOffset.UtcNow, 20 * Math.Log10(_recorder.MaxAmplitude)) };
        }

        protected override void PollingStopped()
        {
            base.PollingStopped();

            try { _recorder.Stop(); }
            catch (Exception) { }
        }
    }
}