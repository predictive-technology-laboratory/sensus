using Photos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSPhotoLibraryChangeObserver : PHPhotoLibraryChangeObserver
	{
		private iOSImageMetadataProbe _probe;

		public iOSPhotoLibraryChangeObserver(iOSImageMetadataProbe probe)
		{
			_probe = probe;
		}

		public override void PhotoLibraryDidChange(PHChange changeInstance)
		{
			
		}
	}
}
