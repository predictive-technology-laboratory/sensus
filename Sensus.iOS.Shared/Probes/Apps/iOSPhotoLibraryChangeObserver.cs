using AVFoundation;
using CoreFoundation;
using ExifLib;
using Foundation;
using MobileCoreServices;
using Photos;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSPhotoLibraryChangeObserver : PHPhotoLibraryChangeObserver
	{
		private iOSImageMetadataProbe _probe;
		private DispatchQueue _dispatchQueue;
		private PHFetchResult _imageFetchResult;
		private PHFetchResult _videoFetchResult;

		public iOSPhotoLibraryChangeObserver(iOSImageMetadataProbe probe, DispatchQueue dispatchQueue)
		{
			_probe = probe;
			_dispatchQueue = dispatchQueue;

			_imageFetchResult = PHAsset.FetchAssets(PHAssetMediaType.Image, new PHFetchOptions());
			_videoFetchResult = PHAsset.FetchAssets(PHAssetMediaType.Video, new PHFetchOptions());
		}

		private void HandleImage(IEnumerable<PHObject> images)
		{
			foreach (PHAsset image in images)
			{
				if (image.MediaType == PHAssetMediaType.Image)
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

						await _probe.CreateAndStoreDatumAsync(path, mimeType, (DateTime)image.CreationDate);
					});
				}
				else if (image.MediaType == PHAssetMediaType.Video)
				{
					PHVideoRequestOptions options = new PHVideoRequestOptions()
					{
						NetworkAccessAllowed = false,
						Version = PHVideoRequestOptionsVersion.Original
					};

					PHImageManager.DefaultManager.RequestAvAsset(image, options, async (a, _, i) =>
					{
						if (a is AVUrlAsset urlAsset)
						{
							string path = urlAsset.Url.Path;
							string extension = urlAsset.Url.PathExtension;
							string uniformTypeIdentifier = UTType.CreatePreferredIdentifier(UTType.TagClassFilenameExtension, extension, null);
							string mimeType = UTType.GetPreferredTag(uniformTypeIdentifier, UTType.TagClassMIMEType); ;

							await _probe.CreateAndStoreDatumAsync(path, mimeType, (DateTime)image.CreationDate);
						}
					});
				}
			}
		}

		public override void PhotoLibraryDidChange(PHChange changeInstance)
		{
			PHFetchResultChangeDetails imageDetails = changeInstance.GetFetchResultChangeDetails(_imageFetchResult);
			PHFetchResultChangeDetails videoDetails = changeInstance.GetFetchResultChangeDetails(_videoFetchResult);
			List<PHObject> insertedObjects = new List<PHObject>();

			if (imageDetails != null && imageDetails.InsertedObjects.Any())
			{
				insertedObjects.AddRange(imageDetails.InsertedObjects);

				_imageFetchResult = imageDetails.FetchResultAfterChanges;
			}

			if (videoDetails != null && videoDetails.InsertedObjects.Any())
			{
				insertedObjects.AddRange(videoDetails.InsertedObjects);

				_videoFetchResult = videoDetails.FetchResultAfterChanges;
			}

			HandleImage(insertedObjects);
		}
	}
}
