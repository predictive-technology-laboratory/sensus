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

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageMetadataProbe : ImageMetadataProbe
	{
		List<AndroidImageFileObserver> _fileObservers;

		public AndroidImageMetadataProbe()
		{
			_fileObservers = new List<AndroidImageFileObserver>
			{
				new AndroidImageFileObserver(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).Path, this)
			};
		}

		protected  override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			_fileObservers.ForEach(x => x.StartWatching());
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			_fileObservers.ForEach(x => x.StopWatching());
		}

		//[JsonIgnore]
		//public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		//private List<AndroidImageFileObserver> _fileObservers;

		//public AndroidImageMetadataProbe()
		//{
		//	_fileObservers = new List<AndroidImageFileObserver>();

		//	_fileObservers.Add(new AndroidImageFileObserver("/storage/emulated/0/DCIM/Camera/"));

		//	_fileObservers[0].StartWatching();
		//}

		//protected async override Task InitializeAsync()
		//{
		//	await base.InitializeAsync();
		//}

		//protected async override Task<List<ImageMetadataDatum>> GetImages()
		//{
		//	List<ImageMetadataDatum> imageMetadata = new List<ImageMetadataDatum>();

		//	if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Storage) == PermissionStatus.Granted)
		//	{
		//		string[] properties = { MediaStore.Images.Media.InterfaceConsts.Data };

		//		global::Android.Net.Uri uri = MediaStore.Images.Media.ExternalContentUri;
		//		CancellationSignal cancel = new CancellationSignal();

		//		ICursor cursor = Application.Context.ContentResolver.Query(uri, properties, MediaStore.Images.Media.InterfaceConsts.DateTaken + $" >= ?", new string[] { (Java.Lang.JavaSystem.CurrentTimeMillis() - PollingSleepDurationMS).ToString() }, MediaStore.Images.Media.InterfaceConsts.DateTaken + " DESC");

		//		while (cursor.MoveToNext())
		//		{
		//			try
		//			{
		//				////Dictionary<string, string> selection = properties.Select(p => new { Property = p, Value = cursor.GetString(cursor.GetColumnIndex(p)) }).ToDictionary(x => x.Property, x => x.Value);

		//				//string path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data)));





		//				//if (File.Exists(path))
		//				//{
		//				//	// handle it
		//				//}
		//				//else
		//				//{
		//				//	//FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);

		//				//	AndroidImageFileObserver fo = new AndroidImageFileObserver(path);
		//				//	fo.StartWatching();

		//				//	_fileObservers.Add(fo);
		//				//}



		//				////using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
		//				////{
		//				////	JpegInfo info = ExifReader.ReadJpeg(fs);
		//				////	double latitude = 0;
		//				////	double longitude = 0;

		//				////	if (info.GpsLatitude != null && info.GpsLongitude != null)
		//				////	{
		//				////		latitude = (Math.Truncate(info.GpsLatitude[0]) + (info.GpsLatitude[1] / 60) + (info.GpsLatitude[2] / 3600)) * (info.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
		//				////		longitude = (Math.Truncate(info.GpsLongitude[0]) + (info.GpsLongitude[1] / 60) + (info.GpsLongitude[2] / 3600)) * (info.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
		//				////	}

		//				////	imageMetadata.Add(new ImageMetadataDatum(info.FileSize, info.Width, info.Height, (int)info.Orientation, info.XResolution, info.YResolution, (int)info.ResolutionUnit, info.IsColor, (int)info.Flash, info.FNumber, info.ExposureTime, latitude, longitude));
		//				////}
		//			}
		//			catch (Exception ex)
		//			{
		//				SensusServiceHelper.Get().Logger.Log("Exception while querying images:  " + ex.Message, LoggingLevel.Normal, GetType());
		//			}
		//		}
		//	}

		//	return imageMetadata;
		//}
	}
}
