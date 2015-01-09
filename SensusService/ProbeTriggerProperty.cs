using System;

namespace SensusService
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ProbeTriggerProperty : Attribute
    {
        private ProbeTriggerPropertyType _type;

        public ProbeTriggerPropertyType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public ProbeTriggerProperty(ProbeTriggerPropertyType type)
        {
            _type = type;
        }        
    }
}
