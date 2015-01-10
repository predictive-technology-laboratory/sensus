using System.Collections.Generic;
using System.Reflection;

namespace SensusService.Probes.User
{
    public interface IScriptProbe
    {
        Protocol Protocol { get; }

        IEnumerable<string> Triggers { get; }

        void AddTrigger(Probe probe, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue, bool change);

        void RemoveTrigger(Probe probe, string triggerKey);
    }
}
