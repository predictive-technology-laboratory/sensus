using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sensus.Context;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Device
{
    public class ListeningPowerConnectProbe : ListeningProbe
    {
        private EventHandler<bool> _powerConnectionChanged;

        public ListeningPowerConnectProbe()
        {
            _powerConnectionChanged = async (sender, connected) =>
            {
                await StoreDatumAsync(new PowerConnectDatum(DateTimeOffset.UtcNow,connected, SensusServiceHelper.Get().BatteryChargePercent));
            };

        }

        public override string DisplayName
        {
            get
            {
                return "Power Connection";
            }
        }
        public override Type DatumType
        {
            get { return typeof(PowerConnectDatum); }
        }

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        protected override  Task StartListeningAsync()
        {
            base.StartListeningAsync();
            SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged += _powerConnectionChanged;
            return Task.CompletedTask;
        }

        protected override Task StopListeningAsync()
        {
            base.StopListeningAsync();
            SensusContext.Current.PowerConnectionChangeListener.PowerConnectionChanged -= _powerConnectionChanged;
            return Task.CompletedTask;
        }


        protected override string DeviceAwakeWarning => throw new NotImplementedException();

        protected override string DeviceAsleepWarning => throw new NotImplementedException();

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

       
    }
}
