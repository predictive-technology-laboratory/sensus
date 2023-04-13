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
using Android.App.Job;
using Android.Content;
using Android.Provider;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uri = Android.Net.Uri;
using Class = Java.Lang.Class;
using Stream = System.IO.Stream;
using Android.Database;
using static Android.Provider.MediaStore;
using Android.Media;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageMetadataProbe : ImageMetadataProbe
	{
		private static JobScheduler _scheduler;
		private static JobInfo _jobInfo;

		private static readonly List<AndroidImageMetadataProbe> _probes;

		static AndroidImageMetadataProbe()
		{
			_probes = new();
		}

		public AndroidImageMetadataProbe()
		{
			_scheduler = (JobScheduler)Application.Context.GetSystemService(Class.FromType(typeof(JobScheduler)));
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<Permissions.StorageRead>() != PermissionStatus.Granted)
			{
				throw new Exception("Failed to obtain Storage permission from user.");
			}
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			if (_scheduler != null)
			{
				lock (_probes)
				{
					_probes.Add(this);
				}

				ScheduleJob();
			}
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			if (_scheduler != null && _jobInfo != null)
			{
				lock (_probes)
				{
					_probes.Remove(this);
				}

				if (_probes.Count == 0 && _scheduler.AllPendingJobs.Contains(_jobInfo))
				{
					_scheduler.Cancel(_jobInfo.Id);
				}
			}
		}

		public static void ScheduleJob()
		{
			int id = _scheduler.AllPendingJobs.Select(x => x.Id).DefaultIfEmpty().Max() + 1 % (int.MaxValue - 1);

			JobInfo.Builder builder = new(id, new ComponentName(Application.Context, Class.FromType(typeof(AndroidImageJobService))));

			builder.AddTriggerContentUri(new JobInfo.TriggerContentUri(Images.Media.ExternalContentUri, TriggerContentUriFlags.NotifyForDescendants));
			builder.AddTriggerContentUri(new JobInfo.TriggerContentUri(Video.Media.ExternalContentUri, TriggerContentUriFlags.NotifyForDescendants));

			_jobInfo = builder.Build();

			_scheduler.Schedule(_jobInfo);
		}

		private static readonly string[] _columns =
		{
				IMediaColumns.MimeType,
				IMediaColumns.Width,
				IMediaColumns.Height,
				IMediaColumns.Size,
				IMediaColumns.Duration,
				IMediaColumns.DateAdded,
		};

		private static readonly Dictionary<string, int> _columnMap = _columns.Select((x, i) => new { Name = x, Index = i }).ToDictionary(x => x.Name, x => x.Index);

		public static async Task CreateDatumAsync(Uri path)
		{
			try
			{
				ICursor cursor = Application.Context.ContentResolver.Query(path, _columns, null, null, null);

				if (cursor.MoveToFirst())
				{
					int added = cursor.GetInt(_columnMap[IMediaColumns.DateAdded]);
					string mimeType = cursor.GetString(_columnMap[IMediaColumns.MimeType]);
					ImageMetadataDatum baseDatum = new(DateTimeOffset.FromUnixTimeSeconds(added).ToLocalTime().DateTime)
					{
						FileSize = cursor.GetInt(_columnMap[IMediaColumns.Size]),
						Width = cursor.GetInt(_columnMap[IMediaColumns.Width]),
						Height = cursor.GetInt(_columnMap[IMediaColumns.Height]),
						Duration = cursor.GetInt(_columnMap[IMediaColumns.Duration]),
						MimeType = mimeType
					};

					bool setExifData = mimeType == JPEG_MIME_TYPE;
					bool storeImage = _probes.Any(x => x.StoreImages || x.StoreVideos);

					if (setExifData || storeImage)
					{
						Stream stream = Application.Context.ContentResolver.OpenInputStream(path);

						if (setExifData)
						{
							SetExifData(stream, ref baseDatum);
						}

						if (storeImage)
						{
							baseDatum.ImageBase64 = await GetImageBase64(stream);
						}

						stream.Close();
					}

					foreach (AndroidImageMetadataProbe probe in _probes)
					{
						if (mimeType.StartsWith(IMAGE_DISCRETE_TYPE) || (mimeType.StartsWith(VIDEO_DISCRETE_TYPE) && probe.StoreVideos))
						{
							Stream stream = Application.Context.ContentResolver.OpenInputStream(path);

							baseDatum.ImageBase64 ??= await GetImageBase64(stream);
						}

						ImageMetadataDatum datum = new(baseDatum.FileSize, baseDatum.Width, baseDatum.Height, baseDatum.Orientation, baseDatum.XResolution, baseDatum.YResolution, baseDatum.ResolutionUnit, baseDatum.IsColor, baseDatum.Flash, baseDatum.FNumber, baseDatum.ExposureTime, baseDatum.Software, baseDatum.Latitude, baseDatum.Longitude, mimeType, baseDatum.ImageBase64, baseDatum.Timestamp);

						await probe.StoreDatumAsync(datum);
					}
				}
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get().Logger.Log($"Exception creating {nameof(ImageMetadataDatum)}: {e.Message}", LoggingLevel.Normal, typeof(AndroidImageMetadataProbe));
			}
		}
	}
}
