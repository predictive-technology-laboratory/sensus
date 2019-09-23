using CoreLocation;
using Foundation;
using Sensus.Probes.Location;
using System;
using System.Threading.Tasks;
using UIKit;

namespace Sensus.iOS.Probes.Location
{
	public class iOSVisitProbe : VisitProbe
	{
		private CLLocationManager _locationManager;

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
			double latitude = args.Visit.Coordinate.Latitude;
			double longitude = args.Visit.Coordinate.Longitude;

			DateTimeOffset timestamp = DateTimeOffset.UtcNow;
			DateTimeOffset? arrivalDate = (DateTime)args.Visit.ArrivalDate;
			DateTimeOffset? departureDate = (DateTime)args.Visit.DepartureDate;

			if (args.Visit.ArrivalDate == NSDate.DistantPast)
			{
				timestamp = (DateTime)args.Visit.DepartureDate;

				arrivalDate = null;
			}
			else if(args.Visit.DepartureDate == NSDate.DistantFuture)
			{
				timestamp = (DateTime)args.Visit.ArrivalDate;

				departureDate = null;
			}

			Task.WaitAny(StoreDatumAsync(new VisitDatum(arrivalDate, departureDate, latitude, longitude, args.Visit.HorizontalAccuracy, timestamp)));
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
