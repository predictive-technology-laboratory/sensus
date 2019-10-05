// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
