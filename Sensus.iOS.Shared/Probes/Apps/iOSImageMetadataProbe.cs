using Sensus.Probes.Apps;
using System.Threading.Tasks;
using Photos;
using CoreFoundation;

namespace Sensus.iOS.Probes.Apps
{
	public class iOSImageMetadataProbe : ImageMetadataProbe
	{
		private iOSPhotoLibraryChangeObserver _changeObserver;
		private DispatchQueue _dispatchQueue;

		public iOSImageMetadataProbe()
		{
			
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();
			_dispatchQueue = new DispatchQueue("com.sensus.imagemetadataprobe");
			_changeObserver = new iOSPhotoLibraryChangeObserver(this, _dispatchQueue);
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
