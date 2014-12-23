
using SensusUI.UiProperties;
using System.Collections.Generic;
using System.Linq;

namespace SensusService.Probes.Apps
{
    public abstract class RunningAppsProbe : PollingProbe
    {
        private int _maxAppsPerPoll;

        [EntryIntegerUiProperty("Max Apps / Poll:", true, 3)]
        public int MaxAppsPerPoll
        {
            get { return _maxAppsPerPoll; }
            set
            {
                if (value != _maxAppsPerPoll)
                {
                    _maxAppsPerPoll = value;
                    OnPropertyChanged();
                }
            }
        }

        protected sealed override string DefaultDisplayName
        {
            get { return "Running Applications"; }
        }

        public RunningAppsProbe()
        {
            _maxAppsPerPoll = 10;
        }

        protected abstract List<RunningAppsDatum> GetRunningAppsData();

        public sealed override IEnumerable<Datum> Poll()
        {
            List<RunningAppsDatum> data = GetRunningAppsData();

            if (data != null && data.Count > _maxAppsPerPoll)
                data = data.GetRange(0, _maxAppsPerPoll);

            return data;
        }
    }
}
