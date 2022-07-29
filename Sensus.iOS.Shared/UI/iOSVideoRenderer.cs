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
using CoreMedia;

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
		private IDisposable _timeControlStatusObserver;
		private IDisposable _playedToEndObserver;

		private class PrivateAVPlayer : AVPlayer
		{
			private VideoPlayer _videoPlayer;

			public PrivateAVPlayer(VideoPlayer videoPlayer)
			{
				_videoPlayer = videoPlayer;
			}

			public override void Seek(CMTime time, CMTime toleranceBefore, CMTime toleranceAfter, AVCompletion completion)
			{
				_videoPlayer.OnVideoSeek(new VideoEventArgs(VideoPlayer.PAUSE, TimeSpan.FromSeconds(time.Seconds)));

				base.Seek(time, toleranceBefore, toleranceAfter, completion);
			}
		}

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

					_player = new PrivateAVPlayer(_videoPlayer);
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
							CGSize size = _playerItem.Asset.Tracks.FirstOrDefault(x => x.NaturalSize.IsEmpty == false)?.NaturalSize ?? CGSize.Empty;

							double ratio = parent.Width / size.Width;

							if (ratio > 1)
							{
								ratio = 1 / ratio;
							}

							parent.HeightRequest = ratio * size.Height;
						}
					}
				});

				_timeControlStatusObserver = _player.AddObserver("timeControlStatus", NSKeyValueObservingOptions.New, o =>
				{
					double time = _playerItem.CurrentTime.Seconds;

					if (_player.TimeControlStatus == AVPlayerTimeControlStatus.Playing && time == 0)
					{
						_videoPlayer.OnVideoStart(new VideoEventArgs(VideoPlayer.START, TimeSpan.FromSeconds(time)));
					}
					else if (_player.TimeControlStatus == AVPlayerTimeControlStatus.Playing && time > 0)
					{
						_videoPlayer.OnVideoResume(new VideoEventArgs(VideoPlayer.RESUME, TimeSpan.FromSeconds(time)));
					}
					else if (_player.TimeControlStatus == AVPlayerTimeControlStatus.Paused && time > 0 && time < _playerItem.Duration.Seconds)
					{
						_videoPlayer.OnVideoPause(new VideoEventArgs(VideoPlayer.PAUSE, TimeSpan.FromSeconds(time)));
					}
				});

				_playedToEndObserver = AVPlayerItem.Notifications.ObserveDidPlayToEndTime((s, e) =>
				{
					if (e.Notification.Object == _playerItem)
					{
						_videoPlayer.OnVideoEnd(new VideoEventArgs(VideoPlayer.END, TimeSpan.FromSeconds(_playerItem.CurrentTime.Seconds)));
					}
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
				_timeControlStatusObserver.Dispose();
				_playedToEndObserver.Dispose();
			}
		}
	}
}
