using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Movement
{
    public abstract class LinearAccelerationProbe : ListeningProbe
    {
        public override string DisplayName
        {
            get { return "Acceleration"; }
        }

        public override Type DatumType
        {
            get { return typeof(LinearAccelerationDatum); }
        }

        public override double? MaxDataStoresPerSecond { get => base.MaxDataStoresPerSecond; set => base.MaxDataStoresPerSecond = value; }

        public override string CollectionDescription => base.CollectionDescription;

        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return true;
            }
        }

        protected override string DeviceAwakeWarning
        {
            get
            {
                return "Devices will use additional power to report all updates.";
            }
        }

        protected override string DeviceAsleepWarning
        {
            get
            {
                return "Devices will sleep and pause updates.";
            }
        }

        protected override bool WillHaveSignificantNegativeImpactOnBattery => base.WillHaveSignificantNegativeImpactOnBattery;

        protected override double RawParticipation => base.RawParticipation;

        protected override long DataRateSampleSize => base.DataRateSampleSize;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override Task ProcessDataAsync(CancellationToken cancellationToken)
        {
            return base.ProcessDataAsync(cancellationToken);
        }

        public override Task ResetAsync()
        {
            return base.ResetAsync();
        }

        public override Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            return base.TestHealthAsync(events);
        }

        public override string ToString()
        {
            return base.ToString();
        }

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

        protected override Task InitializeAsync()
        {
            return base.InitializeAsync();
        }

        protected override Task ProtectedStartAsync()
        {
            return base.ProtectedStartAsync();
        }

        protected override Task ProtectedStopAsync()
        {
            return base.ProtectedStopAsync();
        }

        protected override Task StartListeningAsync()
        {
            return base.StartListeningAsync();
        }

        protected override Task StopListeningAsync()
        {
            return base.StopListeningAsync();
        }
    }
}
