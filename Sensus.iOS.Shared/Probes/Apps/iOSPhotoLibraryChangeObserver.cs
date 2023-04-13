using AVFoundation;
using Foundation;
using MobileCoreServices;
using Photos;
using System;
using System.Linq;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSPhotoLibraryChangeObserver : PHPhotoLibraryChangeObserver
	{
		private iOSImageMetadataProbe _probe;
		private PHFetchResult _imageFetchResult;
		private PHFetchResult _videoFetchResult;

		public iOSPhotoLibraryChangeObserver(iOSImageMetadataProbe probe)
		{
			_probe = probe;

			_imageFetchResult = PHAsset.FetchAssets(PHAssetMediaType.Image, new PHFetchOptions());
			_videoFetchResult = PHAsset.FetchAssets(PHAssetMediaType.Video, new PHFetchOptions());
		}

		public override void PhotoLibraryDidChange(PHChange changeInstance)
		{
			PHFetchResultChangeDetails imageDetails = changeInstance.GetFetchResultChangeDetails(_imageFetchResult);
			PHFetchResultChangeDetails videoDetails = changeInstance.GetFetchResultChangeDetails(_videoFetchResult);

			if (imageDetails != null && imageDetails.InsertedObjects.Any())
			{
				_imageFetchResult = imageDetails.FetchResultAfterChanges;

				foreach (PHAsset image in imageDetails.InsertedObjects.OfType<PHAsset>())
				{
					PHImageRequestOptions options = new PHImageRequestOptions()
					{
						NetworkAccessAllowed = false,
						ResizeMode = PHImageRequestOptionsResizeMode.Exact,
						Version = PHImageRequestOptionsVersion.Original
					};

					PHImageManager.DefaultManager.RequestImageData(image, options, async (d, t, o, i) =>
					{
						string path = ((NSUrl)i["PHImageFileURLKey"])?.Path;
						string mimeType = UTType.GetPreferredTag(t, UTType.TagClassMIMEType);

						await _probe.CreateAndStoreDatumAsync(path, (int)image.PixelWidth, (int)image.PixelHeight, mimeType, (DateTime)image.CreationDate);
					});
				}
			}

			if (videoDetails != null && videoDetails.InsertedObjects.Any())
			{
				_videoFetchResult = videoDetails.FetchResultAfterChanges;

				foreach (PHAsset video in videoDetails.InsertedObjects.OfType<PHAsset>())
				{
					PHVideoRequestOptions options = new PHVideoRequestOptions()
					{
						NetworkAccessAllowed = false,
						Version = PHVideoRequestOptionsVersion.Original
					};

					PHImageManager.DefaultManager.RequestAvAsset(video, options, async (a, _, i) =>
					{
						if (a is AVUrlAsset urlAsset)
						{
							string path = urlAsset.Url.Path;
							string extension = urlAsset.Url.PathExtension;

							string uniformTypeIdentifier = UTType.CreatePreferredIdentifier(UTType.TagClassFilenameExtension, extension, null);
							string mimeType = UTType.GetPreferredTag(uniformTypeIdentifier, UTType.TagClassMIMEType);

							await _probe.CreateAndStoreDatumAsync(path, (int)video.PixelWidth, (int)video.PixelHeight, mimeType, (int)(video.Duration * 1000), (DateTime)video.CreationDate);
						}
					});
				}
			}
		}
	}
}
