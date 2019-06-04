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

using Android;
using Android.App;
using Android.Database;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Support.V4.Content;
using Newtonsoft.Json;
using Plugin.Permissions.Abstractions;
using Sensus.Probes.Apps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExifLib;
using System.IO;
using Syncfusion.SfChart.XForms;
using System;
using File = Java.IO.File;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageMetadataProbe : ImageMetadataProbe
	{
		//List<AndroidImageFileObserver> _fileObservers;

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			//_fileObservers = new List<AndroidImageFileObserver>
			//{
			//	new AndroidImageFileObserver(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).CanonicalPath, this)
			//};
		}

		public async Task CreateAndStoreDatumAsync(string path, DateTime timestamp)
		{
			if (new File(path).Exists())
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					JpegInfo info = ExifReader.ReadJpeg(fs);
					double latitude = 0;
					double longitude = 0;

					if (info.GpsLatitude != null && info.GpsLongitude != null)
					{
						latitude = (Math.Truncate(info.GpsLatitude[0]) + (info.GpsLatitude[1] / 60) + (info.GpsLatitude[2] / 3600)) * (info.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
						longitude = (Math.Truncate(info.GpsLongitude[0]) + (info.GpsLongitude[1] / 60) + (info.GpsLongitude[2] / 3600)) * (info.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
					}

					string imageBase64 = null;
					//fs.Position = 0;


					//if(_probe.CollectImages)
					//{

					//}

					await StoreDatumAsync(new ImageMetadataDatum(info.FileSize, info.Width, info.Height, (int)info.Orientation, info.XResolution, info.YResolution, (int)info.ResolutionUnit, info.IsColor, (int)info.Flash, info.FNumber, info.ExposureTime, info.Software, latitude, longitude, imageBase64, timestamp));
				}
			}
		}

		protected  override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			AndroidImageJobService.Schedule(this);

			//_fileObservers.ForEach(x => x.StartWatching());
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			AndroidImageJobService.Unschedule();

			//_fileObservers.ForEach(x => x.StopWatching());
		}


	}
}
