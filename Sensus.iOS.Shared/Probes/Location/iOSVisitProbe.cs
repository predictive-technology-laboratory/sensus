using CoreLocation;
using Sensus.Probes;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Sensus.iOS.Probes.Location
{
	public class iOSVisitProbe : ListeningProbe
	{
		private CLLocationManager _locationManager;

		protected override bool DefaultKeepDeviceAwake => false;

		public override Type DatumType => typeof(VisitDatum);

		public override string DisplayName => "Visits";

		protected override string DeviceAwakeWarning => "";

		protected override string DeviceAsleepWarning => "";

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

		public iOSVisitProbe()
		{
			_locationManager = new CLLocationManager();

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				_locationManager.DidVisit += OnVisit;
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
			{
				_locationManager.AllowsBackgroundLocationUpdates = true;
			}
		}

		private void OnVisit(object sender, CLVisitedEventArgs args)
		{
			//CLGeocoder geocoder = new CLGeocoder();

			double latitude = args.Visit.Coordinate.Latitude;
			double longitude = args.Visit.Coordinate.Longitude;

			//geocoder.ReverseGeocodeLocation(new CLLocation(latitude, longitude), async (p, e) =>
			//{
			//	if (e != null)
			//	{
			//		SensusServiceHelper.Get().Logger.Log($"Failed to get visit location: {e.LocalizedDescription}", LoggingLevel.Normal, GetType());
			//	}
			//	else
			//	{

			//	}

			//	await StoreDatumAsync(new VisitDatum((DateTime)args.Visit.ArrivalDate, (DateTime)args.Visit.DepartureDate, latitude, longitude, args.Visit.HorizontalAccuracy, DateTimeOffset.UtcNow));
			//});

			Task.WaitAny(StoreDatumAsync(new VisitDatum((DateTime)args.Visit.ArrivalDate, (DateTime)args.Visit.DepartureDate, latitude, longitude, args.Visit.HorizontalAccuracy, DateTimeOffset.UtcNow)));
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0) && CLLocationManager.LocationServicesEnabled)
			{
				_locationManager.RequestAlwaysAuthorization();
				_locationManager.StartMonitoringVisits();
			}
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0) && CLLocationManager.LocationServicesEnabled)
			{
				_locationManager.StopMonitoringVisits();
			}
		}
	}
}
