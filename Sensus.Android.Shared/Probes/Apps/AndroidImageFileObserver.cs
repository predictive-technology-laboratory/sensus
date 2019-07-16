using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Webkit;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AggregateException = System.AggregateException;
using File = Java.IO.File;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageFileObserver : FileObserver
	{
		private string _path;
		private List<AndroidImageFileObserver> _fileObservers;
		private AndroidImageMetadataProbe _probe;
		private HashSet<string> _paths;

		public AndroidImageFileObserver(string path, AndroidImageMetadataProbe probe) : base(path, FileObserverEvents.Create | FileObserverEvents.CloseWrite | FileObserverEvents.MovedTo)
		{
			_path = path;
			_fileObservers = new List<AndroidImageFileObserver>();
			_probe = probe;
			_paths = new HashSet<string>();

			File pathFile = new File(path);
			File[] files = (pathFile.ListFiles() ?? new File[0]).Where(x => x.IsDirectory).ToArray();

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
			if (string.IsNullOrWhiteSpace(path) == false)
			{
				string fullPath = Path.Combine(_path, path);

				if (e == FileObserverEvents.Create)
				{
					_paths.Add(fullPath);
				}
				else if (e == FileObserverEvents.MovedTo || _paths.Contains(fullPath))
				{
					_paths.Remove(fullPath);

					try
					{
						ICursor cursor = null;
						string fileExtension = MimeTypeMap.GetFileExtensionFromUrl(path);
						string mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(fileExtension);
						DateTime timestamp = DateTime.UtcNow;
						bool createDatum = false;

						if (mimeType != null)
						{
							// check if the file is an image or a video and then query the appropriate content uri
							if (mimeType.StartsWith(ImageMetadataProbe.IMAGE_DISCRETE_TYPE))
							{
								cursor = Application.Context.ContentResolver.Query(MediaStore.Images.Media.ExternalContentUri, new string[] { MediaStore.Images.Media.InterfaceConsts.DateTaken }, MediaStore.Images.Media.InterfaceConsts.Data + $" = ?", new string[] { fullPath }, MediaStore.Images.Media.InterfaceConsts.DateTaken + " DESC LIMIT 1");

								createDatum = cursor.MoveToNext();

								if (createDatum)
								{
									timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DateTaken))).DateTime;
								}
							}
							else if (mimeType.StartsWith(ImageMetadataProbe.VIDEO_DISCRETE_TYPE))
							{
								cursor = Application.Context.ContentResolver.Query(MediaStore.Video.Media.ExternalContentUri, new string[] { MediaStore.Video.Media.InterfaceConsts.DateTaken }, MediaStore.Video.Media.InterfaceConsts.Data + $" = ?", new string[] { fullPath }, MediaStore.Video.Media.InterfaceConsts.DateTaken + " DESC LIMIT 1");

								createDatum = cursor.MoveToNext();

								if (createDatum)
								{
									timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateTaken))).DateTime;

								}
							}

							if (createDatum)
							{
								Task.Run(async () =>
								{
									await _probe.CreateAndStoreDatumAsync(fullPath, mimeType, timestamp);
								});
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
}
