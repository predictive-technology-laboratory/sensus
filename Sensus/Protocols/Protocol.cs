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
        private string _description;
        private List<Probe> _probes;

        public List<Probe> Probes
        {
            get { return _probes; }
        }

        public string Description
        {
            get { return _description; }
        }

        public Protocol(string description, bool addAllProbes)
        {
            _description = description;

            if (addAllProbes)
                foreach (Probe probe in Probe.GetAll())
                    AddProbe(probe);
            else
                _probes = new List<Probe>();
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
