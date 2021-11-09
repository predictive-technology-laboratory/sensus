using Xamarin.Forms;
using Sensus;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using Sensus.iOS.UI;
using AVFoundation;
using AVKit;
using Foundation;
using CoreGraphics;
using System;
using System.Linq;

[assembly: ExportRenderer(typeof(VideoPlayer), typeof(iOSVideoRenderer))]

namespace Sensus.iOS.UI
{
	public class iOSVideoRenderer : ViewRenderer<VideoPlayer, UIView>
	{
		private VideoPlayer _videoPlayer;
		private AVPlayer _player;
		private AVPlayerItem _playerItem;
		private AVPlayerViewController _playerViewController;
		private IDisposable _statusObserver;
		private IDisposable _rateObserver;
		private IDisposable _playedToEndObserver;

		protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				_videoPlayer = e.NewElement;

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
					_playerItem = new AVPlayerItem(NSUrl.FromFilename(fileSource.Path));
				}
				else if (e.NewElement.Source is VideoPlayer.UrlSource urlSource)
				{
					_playerItem = new AVPlayerItem(new NSUrl(urlSource.Url));
				}

				_statusObserver = _playerItem.AddObserver("status", NSKeyValueObservingOptions.New, o =>
				{
					if (_player.Status == AVPlayerStatus.ReadyToPlay)
					{
						AVAudioSession audioSession = AVAudioSession.SharedInstance();

						audioSession.SetCategory(AVAudioSessionCategory.Playback);
						audioSession.SetMode(AVAudioSession.ModeMoviePlayback, out _);
						audioSession.SetActive(true);

						if (Element.Parent is View parent && parent.Parent is ContentView == false)
						{
							CGSize size = _playerItem.Asset.Tracks.FirstOrDefault()?.NaturalSize ?? CGSize.Empty;

							double ratio = size.Width / parent.Width;

							if (ratio > 1)
							{
								ratio = 1 / ratio;
							}

							parent.HeightRequest = ratio * size.Height;
						}
					}
				});

				_rateObserver = _playerItem.AddObserver("rate", NSKeyValueObservingOptions.New, o =>
				{
					if (_player.Rate == 0)
					{
						_videoPlayer.OnVideoEnd(new VideoEventArgs(VideoPlayer.PAUSE, TimeSpan.FromSeconds(_playerItem.CurrentTime.Seconds)));
					}
					else if (_player.Rate > 0)
					{

					}

					//_videoPlayer.On
				});

				_playedToEndObserver = _playerItem.AddObserver(AVPlayerItem.DidPlayToEndTimeNotification, NSKeyValueObservingOptions.New, o =>
				{
					_videoPlayer.OnVideoEnd(new VideoEventArgs(VideoPlayer.END, TimeSpan.FromSeconds(_playerItem.CurrentTime.Seconds)));
				});

				_player.ReplaceCurrentItemWithPlayerItem(_playerItem);
			}

			if (e.OldElement != null)
			{
				if (_player.Rate > 0)
				{
					_player.Pause();
				}

				_player.ReplaceCurrentItemWithPlayerItem(null);
				AVAudioSession.SharedInstance().SetActive(false);

				_statusObserver.Dispose();
				_rateObserver.Dispose();
				_playedToEndObserver.Dispose();
			}
		}
	}
}
