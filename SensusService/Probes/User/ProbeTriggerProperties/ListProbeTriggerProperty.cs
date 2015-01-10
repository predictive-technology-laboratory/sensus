using System.Collections.Generic;

namespace SensusService.Probes.User.ProbeTriggerProperties
{
    public class ListProbeTriggerProperty : ProbeTriggerProperty
    {
        private List<object> _items;

        public List<object> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public ListProbeTriggerProperty(List<object> items)
        {
            _items = items;
        }
    }
}
