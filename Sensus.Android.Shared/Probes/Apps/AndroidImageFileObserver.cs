using Android.OS;
using Android.Runtime;
using System.Collections.Generic;
using Java.IO;
using System.Linq;
using Sensus.Probes.Apps;
using ExifLib;
using System.IO;
using System;
using AggregateException = System.AggregateException;
using File = Java.IO.File;
using Android.Provider;
using Android.App;
using Android.Database;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageFileObserver : FileObserver
	{
		private string _path;
		private List<AndroidImageFileObserver> _fileObservers;
		private Dictionary<string, DateTime> _pendingFileNames;
		private AndroidImageMetadataProbe _probe;

		public AndroidImageFileObserver(string path, AndroidImageMetadataProbe probe) : base(path, FileObserverEvents.Create | FileObserverEvents.CloseWrite)
		{
			_path = path;
			_fileObservers = new List<AndroidImageFileObserver>();
			_pendingFileNames = new Dictionary<string, DateTime>();
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
			if (e == FileObserverEvents.Create)
			{
				_pendingFileNames.Add(path, DateTime.Now);
			}
			else if (e == FileObserverEvents.CloseWrite && _pendingFileNames.TryGetValue(path, out DateTime timestamp))
			{
				// do something with the file...

				//_imageEvent(new ImageMetadataDatum(0, 0, 0, 0, 0, 0, 0, true, 0, 0, 0, 0, 0));

				try
				{
					using (FileStream fs = new FileStream(Path.Combine(_path, path), FileMode.Open, FileAccess.Read))
					{
						JpegInfo info = ExifReader.ReadJpeg(fs);
						double latitude = 0;
						double longitude = 0;

						if (info.GpsLatitude != null && info.GpsLongitude != null)
						{
							latitude = (Math.Truncate(info.GpsLatitude[0]) + (info.GpsLatitude[1] / 60) + (info.GpsLatitude[2] / 3600)) * (info.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
							longitude = (Math.Truncate(info.GpsLongitude[0]) + (info.GpsLongitude[1] / 60) + (info.GpsLongitude[2] / 3600)) * (info.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
						}

						_probe.StoreDatumAsync(new ImageMetadataDatum(info.FileSize, info.Width, info.Height, (int)info.Orientation, info.XResolution, info.YResolution, (int)info.ResolutionUnit, info.IsColor, (int)info.Flash, info.FNumber, info.ExposureTime, latitude, longitude, timestamp)).Wait();
					}
				}
				catch(AggregateException ex)
				{
					throw ex.Flatten().InnerExceptions.First();
				}

				_pendingFileNames.Remove(path);
			}
		}
	}
}
