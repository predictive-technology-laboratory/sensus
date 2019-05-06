using System;
using System.Collections.Generic;
using System.Text;
using Syncfusion.SfChart.XForms;

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

        public override string CollectionDescription => base.CollectionDescription;

        protected override bool DefaultKeepDeviceAwake => throw new NotImplementedException();

        protected override string DeviceAwakeWarning => throw new NotImplementedException();

        protected override string DeviceAsleepWarning => throw new NotImplementedException();

        protected override double RawParticipation => base.RawParticipation;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void Reset()
        {
            base.Reset();
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            return base.TestHealth(ref error, ref warning, ref misc);
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

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void StartListening()
        {
            throw new NotImplementedException();
        }

        protected override void StopListening()
        {
            throw new NotImplementedException();
        }
    }
}
