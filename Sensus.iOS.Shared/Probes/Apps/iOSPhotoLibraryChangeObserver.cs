using Photos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSPhotoLibraryChangeObserver : PHPhotoLibraryChangeObserver
	{
		private iOSImageMetadataProbe _probe;
		private List<PHFetchResult> _fetchResults;

		public iOSPhotoLibraryChangeObserver(iOSImageMetadataProbe probe)
		{
			_probe = probe;
			_fetchResults = new List<PHFetchResult>();
			
		}

		public override void PhotoLibraryDidChange(PHChange changeInstance)
		{
			//PHFetchResultChangeDetails details = changeInstance.GetFetchResultChangeDetails(BaseF)

			//foreach(PHObject image in details.)
			//{

			//}
		}
	}
}
