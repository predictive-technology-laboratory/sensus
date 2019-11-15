using Xamarin.Forms;
using Sensus;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Sensus.iOS.UI;
using AVFoundation;
using AVKit;
using Foundation;

[assembly: ExportRenderer(typeof(VideoPlayer), typeof(iOSVideoRenderer))]

namespace Sensus.iOS.UI
{
	public class iOSVideoRenderer : ViewRenderer<VideoPlayer, UIView>
	{
		private AVPlayer _player;
		private AVPlayerItem _playerItem;
		private AVPlayerViewController _playerViewController;

		protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					_playerViewController = new AVPlayerViewController()
					{
						ShowsPlaybackControls = true
					};

					_player = new AVPlayer();
					_playerViewController.Player = _player;

					SetNativeControl(_playerViewController.View);
				}

				if (e.NewElement.Source is VideoPlayer.FileSource fileSource)
				{
					_playerItem = new AVPlayerItem(new NSUrl(fileSource.Path));
				}
				else if (e.NewElement.Source is VideoPlayer.UrlSource urlSource)
				{
					_playerItem = new AVPlayerItem(new NSUrl(urlSource.Url));
				}

				_player.ReplaceCurrentItemWithPlayerItem(_playerItem);
			}

			if (e.OldElement != null)
			{

			}
		}
	}
}
