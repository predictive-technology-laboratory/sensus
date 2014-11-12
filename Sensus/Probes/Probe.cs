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
        #region static members
        /// <summary>
        /// Gets a list of all probes, uninitialized and instatiated with their default parameters.
        /// </summary>
        /// <returns></returns>
        public static List<Probe> GetAll()
        {
            return Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Probe))).Select(t => Activator.CreateInstance(t) as Probe).ToList();
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private int _id;
        private string _name;
        private bool _enabled;
        private ProbeState _state;
        private HashSet<Datum> _collectedData;
        private AutoResetEvent _dataReceivedWaitHandle;

        public int Id
        {
            get { return _id; }
        }

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

        public HashSet<Datum> CollectedData
        {
            get { return _collectedData; }
        }

        protected AutoResetEvent DataReceivedWaitHandle
        {
            get { return _dataReceivedWaitHandle; }
        }

        protected abstract string DisplayName { get; }

        public Probe()
        {
            _id = -1;
            _name = DisplayName;
            _enabled = false;
            _state = ProbeState.Uninitialized;
            _collectedData = new HashSet<Datum>();
            _dataReceivedWaitHandle = new AutoResetEvent(false);
        }

        public virtual ProbeState Initialize()
        {
            _state = ProbeState.Initializing;
            _id = 1;
            _collectedData.Clear();

            return _state;
        }

        public abstract void Test();

        public abstract void Start();

        public void ClearCommittedData(IEnumerable<Datum> data)
        {
            lock (_collectedData)
                foreach (Datum d in data)
                    _collectedData.Remove(d);
        }

        public abstract void Stop();

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
