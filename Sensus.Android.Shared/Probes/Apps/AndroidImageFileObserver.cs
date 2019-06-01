using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using ExifLib;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AggregateException = System.AggregateException;
using File = Java.IO.File;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageFileObserver : FileObserver
	{
		private string _path;
		private List<AndroidImageFileObserver> _fileObservers;
		private AndroidImageMetadataProbe _probe;

		public AndroidImageFileObserver(string path, AndroidImageMetadataProbe probe) : base(path, FileObserverEvents.CloseWrite | FileObserverEvents.MovedTo)
		{
			_path = path;
			_fileObservers = new List<AndroidImageFileObserver>();
			_probe = probe;

			File pathFile = new File(path);
			File[] files = pathFile.ListFiles().Where(x => x.IsDirectory).ToArray();

			foreach (File file in files)
			{
				_fileObservers.Add(new AndroidImageFileObserver(file.CanonicalPath, probe));
			}
		}

		public override void StartWatching()
		{
			base.StartWatching();

			_fileObservers.ForEach(x => x.StartWatching());
		}

		public override void StopWatching()
		{
			_fileObservers.ForEach(x => x.StopWatching());

			base.StopWatching();
		}

		public override void OnEvent([GeneratedEnum] FileObserverEvents e, string path)
		{
			string fullPath = Path.Combine(_path, path);

			ICursor cursor = Application.Context.ContentResolver.Query(MediaStore.Images.Media.ExternalContentUri, new string[] { MediaStore.Images.Media.InterfaceConsts.DateTaken }, MediaStore.Images.Media.InterfaceConsts.Data + $" = ?", new string[] { fullPath }, MediaStore.Images.Media.InterfaceConsts.DateTaken + " DESC LIMIT 1");

			if (cursor.MoveToNext())
			{
				DateTime timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DateTaken))).DateTime;
				
				try
				{
					if (new File(fullPath).Exists())
					{
						using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
						{
							JpegInfo info = ExifReader.ReadJpeg(fs);
							double latitude = 0;
							double longitude = 0;

							if (info.GpsLatitude != null && info.GpsLongitude != null)
							{
								latitude = (Math.Truncate(info.GpsLatitude[0]) + (info.GpsLatitude[1] / 60) + (info.GpsLatitude[2] / 3600)) * (info.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
								longitude = (Math.Truncate(info.GpsLongitude[0]) + (info.GpsLongitude[1] / 60) + (info.GpsLongitude[2] / 3600)) * (info.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
							}

							fs.Position = 0;

							

							_probe.StoreDatumAsync(new ImageMetadataDatum(info.FileSize, info.Width, info.Height, (int)info.Orientation, info.XResolution, info.YResolution, (int)info.ResolutionUnit, info.IsColor, (int)info.Flash, info.FNumber, info.ExposureTime, latitude, longitude, timestamp)).Wait();
						}
					}
				}
				catch (AggregateException ex)
				{
					SensusServiceHelper.Get().Logger.Log("Exception while querying images:  " + ex.Flatten().InnerExceptions.First().Message, LoggingLevel.Normal, GetType());
				}
				catch (Exception ex)
				{
					SensusServiceHelper.Get().Logger.Log("Exception while querying images:  " + ex.Message, LoggingLevel.Normal, GetType());
				}
			}
		}
	}
}
