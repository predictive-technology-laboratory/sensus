using Sensus.Probes;

namespace Sensus.Exceptions
{
    public class InvalidProbeStateException : ProbeException
    {
        private ProbeState _targetState;

        public ProbeState TargetState
        {
            get { return _targetState; }
        }

        public InvalidProbeStateException(Probe probe, ProbeState targetState)
            : base(probe, "Cannot transition probe \"" + probe + "\" from state \"" + probe.State + "\" to state \"" + targetState + "\".")
        {
            _targetState = targetState;
        }
    }
}
