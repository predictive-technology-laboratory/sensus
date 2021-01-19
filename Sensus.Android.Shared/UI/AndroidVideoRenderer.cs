using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Sensus.Android.UI;
using Sensus;
using Android.Media;
using Android.Views;
using System;
using RelativeLayout = Android.Widget.RelativeLayout;
using Uri = Android.Net.Uri;
using View = Xamarin.Forms.View;
using AndroidContext = Android.Content.Context;

[assembly: ExportRenderer(typeof(VideoPlayer), typeof(AndroidVideoRenderer))]

namespace Sensus.Android.UI
{
	public class AndroidVideoRenderer : ViewRenderer<VideoPlayer, RelativeLayout>
	{
		private PrivateVideoView _videoView;
		private VideoPlayer _videoPlayer;
		private MediaController _mediaController;

		private class PrivateVideoView : VideoView, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnCompletionListener, MediaPlayer.IOnSeekCompleteListener, ViewTreeObserver.IOnScrollChangedListener
		{
			private readonly VideoPlayer _videoPlayer;
			private MediaPlayer _mediaPlayer;
			private MediaController _mediaController;

			public PrivateVideoView(AndroidContext context, VideoPlayer videoPlayer) : base(context)
			{
				_videoPlayer = videoPlayer;

				SetOnPreparedListener(this);
				SetOnCompletionListener(this);

				ViewTreeObserver.AddOnScrollChangedListener(this);
			}

			public override void SetMediaController(MediaController controller)
			{
				_mediaController = controller;

				base.SetMediaController(controller);
			}

			public void OnPrepared(MediaPlayer mp)
			{
				mp.SetOnSeekCompleteListener(this);
				
				SetMediaPlayer(mp);
			}

			public void OnSeekComplete(MediaPlayer mp)
			{
				_videoPlayer.OnVideoSeek(new VideoEventArgs(VideoPlayer.SEEK, TimeSpan.FromMilliseconds(mp.CurrentPosition)));
			}

			public void OnCompletion(MediaPlayer mp)
			{
				_videoPlayer.OnVideoEnd(new VideoEventArgs(VideoPlayer.END, TimeSpan.FromMilliseconds(mp.CurrentPosition)));
			}

			public override bool OnTouchEvent(MotionEvent e)
			{
				if (_mediaController != null && e.Action == MotionEventActions.Up)
				{
					_mediaController.Show();
				}

				return true;
			}

			public void OnScrollChanged()
			{
				if (_mediaController != null)
				{
					_mediaController.Hide();
				}
			}

			public override void Start()
			{
				if (_mediaPlayer.CurrentPosition == 0)
				{
					_videoPlayer.OnVideoStart(new VideoEventArgs(VideoPlayer.START, TimeSpan.FromMilliseconds(_mediaPlayer.CurrentPosition)));
				}
				else
				{
					_videoPlayer.OnVideoResume(new VideoEventArgs(VideoPlayer.RESUME, TimeSpan.FromMilliseconds(_mediaPlayer.CurrentPosition)));
				}

				base.Start();
			}

			public override void Pause()
			{
				_videoPlayer.OnVideoPause(new VideoEventArgs(VideoPlayer.PAUSE, TimeSpan.FromMilliseconds(_mediaPlayer.CurrentPosition)));

				base.Pause();
			}

			public void SetMediaPlayer(MediaPlayer mediaPlayer)
			{
				_mediaPlayer = mediaPlayer;

				LayoutParameters.Height = LayoutParams.MatchParent;

				if (_videoPlayer.Parent is View parent && parent.Parent is ContentView == false)
				{
					double ratio = mediaPlayer.VideoWidth / parent.Width;

					if (ratio > 1)
					{
						ratio = 1 / ratio;
					}

					parent.HeightRequest = ratio * mediaPlayer.VideoHeight;
				}
			}
		}

		public AndroidVideoRenderer(AndroidContext context) : base(context)
		{

		}

		protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				_videoPlayer = e.NewElement;

				if (Control == null)
				{
					RelativeLayout layout = new RelativeLayout(Context);
					RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, 1);

					_mediaController = new MediaController(Context);
					_videoView = new PrivateVideoView(Context, _videoPlayer);

					layout.AddView(_videoView);
					layoutParams.AddRule(LayoutRules.CenterInParent);

					_videoView.LayoutParameters = layoutParams;

					_videoView.SetMediaController(_mediaController);

					SetNativeControl(layout);
				}

				if (e.NewElement.Source is VideoPlayer.FileSource fileSource)
				{
					_videoView.SetVideoPath(fileSource.Path);
				}
				else if(e.NewElement.Source is VideoPlayer.UrlSource urlSource)
				{
					_videoView.SetVideoURI(Uri.Parse(urlSource.Url));
				}
			}

			if (e.OldElement != null)
			{

			}
		}
	}
}
