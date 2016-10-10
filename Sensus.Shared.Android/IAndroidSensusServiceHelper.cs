using Android.Hardware;

namespace Sensus.Service.Android
{
    interface IAndroidSensusServiceHelper
    {
        int WakeLockAcquisitionCount { get; }

        void StopAnroidSensusService();

        SensorManager GetSensorManager();
    }
}
