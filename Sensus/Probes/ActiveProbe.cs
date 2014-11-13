using Sensus.Exceptions;
using Sensus.Probes.Parameters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sensus.Probes
{
    /// <summary>
    /// A probe that polls a data source for samples on a predetermined schedule.
    /// </summary>
    public abstract class ActiveProbe : Probe
    {
        private int _sleepDurationMS;
        private Thread _pollThread;
        private AutoResetEvent _pollTrigger;

        [EntryIntegerProbeParameter("Sleep Duration (Milliseconds):", true)]
        public int SleepDurationMS
        {
            get { return _sleepDurationMS; }
            set
            {
                if (value != _sleepDurationMS)
                {
                    _sleepDurationMS = value;
                    OnPropertyChanged();
                }
            }
        }

        public ActiveProbe()
        {
            _sleepDurationMS = 1000;
            _pollTrigger = new AutoResetEvent(true);
        }

        public override void Test()
        {
            if (Poll() == null)
                throw new ProbeTestException(this, "Failed to poll probe.");
        }

        public override void Start()
        {
            lock (this)
            {
                if (State != ProbeState.Initialized)
                    throw new InvalidProbeStateException(this, ProbeState.Started);

                State = ProbeState.Started;  // change state here in case of multiple back-to-back calls to Start, which should not be allowed.
            }

            _pollThread = new Thread(new ThreadStart(() =>
                {
                    while (State == ProbeState.Started)
                    {
                        _pollTrigger.WaitOne(_sleepDurationMS);

                        if (State == ProbeState.Started)
                            StoreDatum(Poll());
                    }
                }));

            _pollThread.Start();
        }

        protected abstract Datum Poll();

        public override void Stop()
        {
            lock (this)
            {
                if (State != ProbeState.Started)
                    throw new InvalidProbeStateException(this, ProbeState.Stopping);

                State = ProbeState.Stopping; // change state here in case of multiple back-to-back calls to Start, which should not be allowed.
            }

            _pollTrigger.Set();  // don't wait for current sleep cycle to end -- wake up immediately
            _pollThread.Join();

            State = ProbeState.Stopped;
        }
    }
}
