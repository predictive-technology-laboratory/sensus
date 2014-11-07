using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
    /// <summary>
    /// Discovers and controls probes.
    /// </summary>
    public class ProbeController
    {
        private List<Probe> _registeredProbes;

        /// <summary>
        /// Constructor
        /// </summary>
        public ProbeController()
        {
            _registeredProbes = new List<Probe>();
        }

        /// <summary>
        /// Discovers probes available on the current device. The returned probes will not be registered with the controller.
        /// </summary>
        /// <returns>List of probes available on the current device, configured with default parameters.</returns>
        public virtual List<Probe> DiscoverProbes()
        {
            return null;
        }

        /// <summary>
        /// Registers a probe with this controller.
        /// </summary>
        /// <param name="probe">Probe to register.</param>
        public void RegisterProbe(Probe probe)
        {
        }

        /// <summary>
        /// Unregisters a probe with this controller.
        /// </summary>
        /// <param name="probe">Probe to unregister.</param>
        public void UnregisterProbe(Probe probe)
        {
        }

        /// <summary>
        /// Begins to poll registered probes.
        /// </summary>
        public virtual void StartPollingProbes()
        {
        }

        /// <summary>
        /// Stops polling probes.
        /// </summary>
        public virtual void StopPollingProbes()
        {
        }
    }
}
