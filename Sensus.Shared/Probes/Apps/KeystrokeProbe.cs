using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;
using Newtonsoft.Json;

namespace Sensus.Probes.Apps
{
    public abstract class KeystrokeProbe : ListeningProbe
    {

        public override string DisplayName
        {
            get
            {
                return "Keystroke";
            }
        }
        public override Type DatumType
        {
            get { return typeof(KeystrokeDatum); }
        }

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting should not be enabled. It does not affect iOS and will unnecessarily reduce battery life on Android.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return null;
            }
        }

        public override double? MaxDataStoresPerSecond { get => base.MaxDataStoresPerSecond; set => base.MaxDataStoresPerSecond = value; }

        public override string CollectionDescription => base.CollectionDescription;

        protected override bool WillHaveSignificantNegativeImpactOnBattery => base.WillHaveSignificantNegativeImpactOnBattery;

        protected override double RawParticipation => base.RawParticipation;

        protected override long DataRateSampleSize => base.DataRateSampleSize;

        
    }
}
