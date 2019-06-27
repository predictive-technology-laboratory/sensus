using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Photos;

namespace Sensus.iOS.Probes.Apps
{
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
	}
}
