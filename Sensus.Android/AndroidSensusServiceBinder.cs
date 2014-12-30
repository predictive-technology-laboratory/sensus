using Android.OS;
using SensusService;

namespace Sensus.Android
{
    public class AndroidSensusServiceBinder : Binder
    {
        private AndroidSensusServiceHelper _sensusServiceHelper;

        public AndroidSensusServiceHelper SensusServiceHelper
        {
            get { return _sensusServiceHelper; }
            set { _sensusServiceHelper = value; }
        }

        public bool IsBound
        {
            get { return _sensusServiceHelper != null; }
        }

        public AndroidSensusServiceBinder(AndroidSensusServiceHelper sensusServiceHelper)
        {
            _sensusServiceHelper = sensusServiceHelper;
        }
    }
}