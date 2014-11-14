using Sensus.Exceptions;
using Sensus.UI.Properties;
using System;
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

        [EntryIntegerUiProperty("Sleep Duration (MS):", true)]
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
            _pollTrigger = new AutoResetEvent(true); // start polling immediately
        }

        public override void Test()
        {
            if (Poll() == null)
                throw new ProbeTestException(this, "Failed to poll probe.");
        }

        public override void Start()
        {
            ChangeState(ProbeState.TestPassed, ProbeState.Started);

            _pollThread = new Thread(new ThreadStart(() =>
                {
                    while (State == ProbeState.Started)
                    {
                        _pollTrigger.WaitOne(_sleepDurationMS);

                        if (State == ProbeState.Started)
                        {
                            Datum d = null;
                            try { d = Poll(); }
                            catch (Exception) { ChangeState(ProbeState.Started, ProbeState.Failed); }

                            StoreDatum(d);
                        }
                    }
                }));

            _pollThread.Start();
        }

        protected abstract Datum Poll();

        public override void Stop()
        {
            ChangeState(ProbeState.Started, ProbeState.Stopping);

            _pollTrigger.Set();  // don't wait for current sleep cycle to end -- wake up immediately so thread can be joined
            _pollThread.Join();

            ChangeState(ProbeState.Stopping, ProbeState.Stopped);
        }
    }
}
