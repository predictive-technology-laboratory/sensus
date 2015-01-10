using System;

namespace SensusService
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ProbeTriggerProperty : Attribute
    {
    }
}
