using System.Collections.Generic;

namespace SensusService.Probes.User.ProbeTriggerProperties
{
    public class ListProbeTriggerProperty : ProbeTriggerProperty
    {
        private object[] _items;

        public object[] Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public ListProbeTriggerProperty(object[] items)
        {
            _items = items;
        }
    }
}
