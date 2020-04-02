using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Sensus.Android.UI;
using Sensus;
using RelativeLayout = Android.Widget.RelativeLayout;
using Uri = Android.Net.Uri;
using Android.Media;
using Java.Interop;
using System;

[assembly: ExportRenderer(typeof(VideoPlayer), typeof(AndroidVideoRenderer))]

namespace Sensus.Android.UI
{
	public class AndroidVideoRenderer : ViewRenderer<VideoPlayer, RelativeLayout>, MediaPlayer.IOnPreparedListener, MediaPlayer.IOnCompletionListener, MediaPlayer.IOnSeekCompleteListener
	{
		private PrivateVideoView _videoView;
		private VideoPlayer _videoPlayer;
		private MediaController _mediaController;
		private View _parent;

		private class PrivateVideoView : VideoView
		{
			private readonly VideoPlayer _videoPlayer;
			private MediaPlayer _mediaPlayer;

			public PrivateVideoView(global::Android.Content.Context context, VideoPlayer videoPlayer) : base(context)
			{
				_videoPlayer = videoPlayer;
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
			}
		}

		public AndroidVideoRenderer(global::Android.Content.Context context) : base(context)
		{

		}

		public void OnPrepared(MediaPlayer mp)
		{
			mp.SetOnSeekCompleteListener(this);
			_videoView.SetMediaPlayer(mp);

			_videoView.LayoutParameters.Height = LayoutParams.MatchParent;

			if (_parent.Parent is ContentView == false)
			{
				double ratio = mp.VideoWidth / _parent.Width;

				if (ratio > 1)
				{
					ratio = 1 / ratio;
				}

				_parent.HeightRequest = ratio * mp.VideoHeight;
			}
		}

		public void OnSeekComplete(MediaPlayer mp)
		{
			_videoPlayer.OnVideoSeek(new VideoEventArgs(VideoPlayer.SEEK, TimeSpan.FromMilliseconds(mp.CurrentPosition)));
		}

		public void OnCompletion(MediaPlayer mp)
		{
			_videoPlayer.OnVideoEnd(new VideoEventArgs(VideoPlayer.END, TimeSpan.FromMilliseconds(mp.CurrentPosition)));
		}

		protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				_videoPlayer = e.NewElement;
				_parent = e.NewElement.Parent as View;

				if (Control == null)
				{
					RelativeLayout layout = new RelativeLayout(Context);
					RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, 1);

					_videoView = new PrivateVideoView(Context, _videoPlayer);
					_mediaController = new MediaController(Context);

					_videoView.SetOnPreparedListener(this);
					_videoView.SetOnCompletionListener(this);

					layout.AddView(_videoView);
					layoutParams.AddRule(LayoutRules.CenterInParent);

					_videoView.LayoutParameters = layoutParams;

					_mediaController.SetMediaPlayer(_videoView);
					_mediaController.SetPrevNextListeners(null, null);
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
