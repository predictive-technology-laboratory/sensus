using Android.OS;

namespace Sensus.Android
{
    public class SensusServiceBinder : Binder
    {
        private AndroidSensusService _service;

        public AndroidSensusService Service
        {
            get { return _service; }
            set { _service = value; }
        }

        public bool IsBound
        {
            get { return _service != null; }
        }

        public SensusServiceBinder(AndroidSensusService service)
        {
            _service = service;
        }
    }
}