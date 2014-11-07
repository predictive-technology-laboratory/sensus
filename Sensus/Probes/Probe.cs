using Sensus.DataStores.Local;
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sensus.Probes
{
    /// <summary>
    /// An abstract probe.
    /// </summary>
    public abstract class Probe
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        public class ProbeParameter : Attribute
        {
            private bool _editable;

            public bool Editable
            {
                get { return _editable; }
            }

            public ProbeParameter(bool editable)
            {
                _editable = editable;
            }
        }

        [ProbeParameter(true)]
        private int _sleepDurationMS;

        private ProbeState _state;
        private AutoResetEvent _pollTrigger;
        private Thread _pollThread;
        private List<Datum> _polledData;
        private AutoResetEvent _dataReceivedEvent;

        public ProbeState State
        {
            get { return _state; }
            set { _state = value; }
        }

        protected List<Datum> PolledData
        {
            get { return _polledData; }
        }

        public AutoResetEvent DataReceivedEvent
        {
            get { return _dataReceivedEvent; }
        }

        public Probe()
        {
            _sleepDurationMS = 1000;
            _state = ProbeState.Uninitialized;
            _pollTrigger = new AutoResetEvent(false);
            _polledData = new List<Datum>();
            _dataReceivedEvent = new AutoResetEvent(false);
        }

        public virtual void Test()
        {
            _polledData.Clear();
        }

        public void StartPolling()
        {
            lock (this)
            {
                if (_state != ProbeState.Initialized)
                    throw new InvalidProbeStateException(this, ProbeState.Polling);

                _state = ProbeState.Polling;
            }

            _pollThread = new Thread(new ThreadStart(() =>
                {
                    while (_state == ProbeState.Polling)
                    {
                        _pollTrigger.WaitOne(_sleepDurationMS);

                        if (_state == ProbeState.Polling)
                            Poll();
                    }
                }));

            _pollThread.Start();
        }

        public void TriggerPoll()
        {
            _pollTrigger.Set();
        }

        protected abstract void Poll();

        public void StopPolling()
        {
            if (_state != ProbeState.Polling)
                throw new InvalidProbeStateException(this, ProbeState.Stopping);

            _state = ProbeState.Stopping;
            _pollTrigger.Set();
            _pollThread.Join();
            _state = ProbeState.Stopped;
        }
    }
}
