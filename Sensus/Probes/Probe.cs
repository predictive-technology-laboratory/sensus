using Sensus.DataStores.Local;
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;

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

        /// <summary>
        /// Gets a list of all probes, uninitialized and instatiated with their default parameters.
        /// </summary>
        /// <returns></returns>
        public static List<Probe> GetAll()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).ToList();
        }

        [ProbeParameter(true)]
        private int _sleepDurationMS;
        private ProbeState _state;
        private AutoResetEvent _pollTrigger;
        private Thread _pollThread;
        private List<Datum> _polledData;
        private AutoResetEvent _dataReceivedWaitHandle;

        public ProbeState State
        {
            get { return _state; }
            set { _state = value; }
        }

        protected List<Datum> PolledData
        {
            get { return _polledData; }
        }

        public AutoResetEvent DataReceivedWaitHandle
        {
            get { return _dataReceivedWaitHandle; }
        }

        public Probe()
        {
            _sleepDurationMS = 1000;
            _state = ProbeState.Uninitialized;
            _pollTrigger = new AutoResetEvent(false);
            _polledData = new List<Datum>();
            _dataReceivedWaitHandle = new AutoResetEvent(false);
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
