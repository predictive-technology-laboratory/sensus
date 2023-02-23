using Sensus.Probes.Apps;
using System.Threading.Tasks;
using Photos;
using System.IO;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Sensus.iOS.Probes.Apps
{
	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
	public class iOSImageMetadataProbe : ImageMetadataProbe
	{
		private iOSPhotoLibraryChangeObserver _changeObserver;

		public iOSImageMetadataProbe()
		{

		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			_changeObserver = new iOSPhotoLibraryChangeObserver(this);
		}

		protected override async Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			if (await PHPhotoLibrary.RequestAuthorizationAsync() == PHAuthorizationStatus.Authorized)
			{
				PHPhotoLibrary.SharedPhotoLibrary.RegisterChangeObserver(_changeObserver);
			}
		}

		protected override async Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			PHPhotoLibrary.SharedPhotoLibrary.UnregisterChangeObserver(_changeObserver);
		}

		public async Task CreateAndStoreDatumAsync(string path, int width, int height, string mimeType, DateTime timestamp)
		{
			ImageMetadataDatum datum = new(timestamp)
			{
				Width = width,
				Height = height,
				MimeType = mimeType
			};

			if (File.Exists(path))
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					if (mimeType == JPEG_MIME_TYPE)
					{
						SetExifData(fs, ref datum);
					}

					if (StoreImages)
					{
						datum.ImageBase64 = await GetImageBase64(fs);
					}
				}
			}

			await StoreDatumAsync(datum);
		}
		public async Task CreateAndStoreDatumAsync(string path, int width, int height, string mimeType, int duration, DateTime timestamp)
		{
			ImageMetadataDatum datum = new(timestamp)
			{
				Width = width,
				Height = height,
				Duration = duration,
				MimeType = mimeType
			};

			if (File.Exists(path))
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					if (StoreVideos)
					{
						datum.ImageBase64 = await GetImageBase64(fs);
					}
				}
			}

			await StoreDatumAsync(datum);
		}
	}
}
