using Sensus.Exceptions;
using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Protocols
{
    /// <summary>
    /// Defines a Sensus protocol.
    /// </summary>
    public class Protocol
    {
        private string _name;
        private List<Probe> _probes;

        public string Name
        {
            get { return _name; }
        }

        public List<Probe> Probes
        {
            get { return _probes; }
        }

        public Protocol(string name, bool addAllProbes)
        {
            _name = name;
            _probes = new List<Probe>();

            if (addAllProbes)
                foreach (Probe probe in Probe.GetAll())
                    AddProbe(probe);
        }

        public void AddProbe(Probe probe)
        {
            _probes.Add(probe);
        }

        public void RemoveProbe(Probe probe)
        {
            _probes.Remove(probe);
        }
    }
}
