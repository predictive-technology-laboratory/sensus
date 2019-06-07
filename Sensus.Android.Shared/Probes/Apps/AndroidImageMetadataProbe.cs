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

using Android.App;
using Android.Database;
using Android.Provider;
using ExifLib;
using Plugin.Permissions.Abstractions;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Environment = Android.OS.Environment;
using File = Java.IO.File;
using Uri = Android.Net.Uri;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageMetadataProbe : ImageMetadataProbe
	{
		List<AndroidImageFileObserver> _fileObservers;
		private const int QUERY_LAST_IMAGE_COUNT = 100;
		private const string JPEG_MIME_TYPE = "image/jpeg";

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Storage) == PermissionStatus.Granted)
			{
				// Different phones and camera apps can save images in different physical locations, so look at the last QUERY_LAST_IMAGE_COUNT number of images and observe the location(s) they were saved in.
				ICursor cursor = Application.Context.ContentResolver.Query(MediaStore.Images.Media.ExternalContentUri, new string[] { MediaStore.Images.Media.InterfaceConsts.Data }, null, null, MediaStore.Images.Media.InterfaceConsts.DateTaken + $" DESC LIMIT {QUERY_LAST_IMAGE_COUNT}");
				List<string> paths = new List<string> { Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).CanonicalPath };

				while (cursor.MoveToNext())
				{
					string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
					Uri uri = Uri.Parse(path);

					path = File.Separator + Path.Combine(uri.PathSegments.Take(uri.PathSegments.Count - 1).ToArray());

					if (paths.Any(x => path.StartsWith(x)) == false)
					{
						paths.Add(path);
					}
				}

				_fileObservers = paths.Distinct().Select(x => new AndroidImageFileObserver(x, this)).ToList();
			}
			else
			{
				throw new Exception("Failed to obtain Storage permission from user.");
			}
		}

		public async Task CreateAndStoreDatumAsync(string path, string mimeType, DateTime timestamp)
		{
			if (new File(path).Exists())
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					string imageBase64 = null;

					if (mimeType == JPEG_MIME_TYPE)
					{
						JpegInfo info = ExifReader.ReadJpeg(fs);

						double latitude = 0;
						double longitude = 0;

						if (info.GpsLatitude != null && info.GpsLongitude != null)
						{
							latitude = (Math.Truncate(info.GpsLatitude[0]) + (info.GpsLatitude[1] / 60) + (info.GpsLatitude[2] / 3600)) * (info.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
							longitude = (Math.Truncate(info.GpsLongitude[0]) + (info.GpsLongitude[1] / 60) + (info.GpsLongitude[2] / 3600)) * (info.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
						}

						if (StoreImages)
						{
							fs.Position = 0;
							byte[] buffer = new byte[fs.Length];
							fs.Read(buffer, 0, (int)fs.Length);

							imageBase64 = Convert.ToBase64String(buffer);
						}

						await StoreDatumAsync(new ImageMetadataDatum(info.FileSize, info.Width, info.Height, (int)info.Orientation, info.XResolution, info.YResolution, (int)info.ResolutionUnit, info.IsColor, (int)info.Flash, info.FNumber, info.ExposureTime, info.Software, latitude, longitude, mimeType, imageBase64, timestamp));
					}
					else // the file is something else...
					{
						if (StoreVideos)
						{
							fs.Position = 0;
							byte[] buffer = new byte[fs.Length];
							fs.Read(buffer, 0, (int)fs.Length);

							imageBase64 = Convert.ToBase64String(buffer);
						}

						await StoreDatumAsync(new ImageMetadataDatum((int)fs.Length, null, null, null, null, null, null, null, null, null, null, null, null, null, mimeType, imageBase64, timestamp));
					}
				}
			}
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			_fileObservers.ForEach(x => x.StartWatching());
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			_fileObservers.ForEach(x => x.StopWatching());
		}


	}
}
