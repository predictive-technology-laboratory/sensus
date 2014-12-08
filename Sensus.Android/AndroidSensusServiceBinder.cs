using Android.OS;
using SensusService;

namespace Sensus.Android
{
    public class AndroidSensusServiceBinder : Binder
    {
        private SensusServiceHelper _sensusServiceHelper;

        public SensusServiceHelper SensusServiceHelper
        {
            get { return _sensusServiceHelper; }
            set { _sensusServiceHelper = value; }
        }

        public bool IsBound
        {
            get { return _sensusServiceHelper != null; }
        }

        public AndroidSensusServiceBinder(SensusServiceHelper sensusServiceHelper)
        {
            _sensusServiceHelper = sensusServiceHelper;
        }
    }
}