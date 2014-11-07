using Sensus.Exceptions;
using Sensus.Probes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Protocols
{
    /// <summary>
    /// Defines a Sensus protocol
    /// </summary>
    public class Protocol
    {
        private List<Probe> _probes;

        public List<Probe> Probes
        {
            get { return _probes; }
        }

        public Protocol()
        {
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
