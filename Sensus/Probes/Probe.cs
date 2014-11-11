using Sensus.DataStores.Local;
using Sensus.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sensus.Probes.Parameters;

namespace Sensus.Probes
{
    /// <summary>
    /// An abstract probe.
    /// </summary>
    public abstract class Probe : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a list of all probes, uninitialized and instatiated with their default parameters.
        /// </summary>
        /// <returns></returns>
        public static List<Probe> GetAll()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).ToList();
        }

        private string _name;
        private bool _enabled;
        private int _sleepDurationMS;
        private ProbeState _state;
        private AutoResetEvent _pollTrigger;
        private Thread _pollThread;
        private List<Datum> _polledData;
        private AutoResetEvent _dataReceivedWaitHandle;

        public string Name
        {
            get { return _name; }
            set
            {
                if (!value.Equals(_name, StringComparison.Ordinal))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public ProbeState State
        {
            get { return _state; }
            set
            {
                if (value != _state)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Datum> PolledData
        {
            get { return _polledData; }
        }

        public AutoResetEvent DataReceivedWaitHandle
        {
            get { return _dataReceivedWaitHandle; }
        }

        protected abstract string FriendlyName { get; }

        public Probe()
        {
            _name = FriendlyName;
            _enabled = false;
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
