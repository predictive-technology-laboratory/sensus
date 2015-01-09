using System.Collections.Generic;

namespace SensusService.Probes.User
{
    public interface IScriptProbe
    {
        Protocol Protocol { get; }

        IEnumerable<string> Triggers { get; }

        void RemoveTrigger(string triggerKey);
    }
}
