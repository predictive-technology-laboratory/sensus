
using SensusUI.UiProperties;
namespace SensusService.Probes.Apps
{
    public abstract class RunningAppsProbe : PollingProbe
    {
        private int _maximumNumber;

        [EntryIntegerUiProperty("Max Apps / Poll:", true, 1)]
        public int MaximumNumber
        {
            get { return _maximumNumber; }
            set
            {
                if (value != _maximumNumber)
                {
                    _maximumNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        protected override string DefaultDisplayName
        {
            get { return "Running Applications"; }
        }
    }
}
