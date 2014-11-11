using Sensus.Exceptions;
using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sensus.Protocols
{
    /// <summary>
    /// Defines a Sensus protocol.
    /// </summary>
    public class Protocol : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        private List<Probe> _probes;
        private bool _running;
        private PropertyChangedEventHandler _notifyWatchersOfProbesChange;

        public string Name
        {
            get { return _name; }
            set
            {
                if(!value.Equals(_name, StringComparison.Ordinal))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Probe> Probes
        {
            get { return _probes; }
        }

        public bool Running
        {
            get { return _running; }
            set
            {
                if (value != _running)
                {
                    _running = value;
                    OnPropertyChanged();
                }
            }
        }

        public Protocol(string name, bool addAllProbes)
        {
            _name = name;
            _probes = new List<Probe>();
            _running = false;
            _notifyWatchersOfProbesChange = (o, e) =>
                {
                    OnPropertyChanged("Probes");
                };

            if (addAllProbes)
                foreach (Probe probe in Probe.GetAll())
                    AddProbe(probe);
        }

        public void AddProbe(Probe probe)
        {
            probe.PropertyChanged += _notifyWatchersOfProbesChange;
            _probes.Add(probe);
        }

        public void RemoveProbe(Probe probe)
        {
            probe.PropertyChanged -= _notifyWatchersOfProbesChange;
            _probes.Remove(probe);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
