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
	public class AndroidVideoRenderer : ViewRenderer<VideoPlayer, RelativeLayout>, MediaPlayer.IOnPreparedListener
	{
		private VideoView _videoView;
		private MediaController _mediaController;
		private View _parent;

		public AndroidVideoRenderer(global::Android.Content.Context context) : base(context)
		{

		}

		public void OnPrepared(MediaPlayer mp)
		{
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

		protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				_parent = e.NewElement.Parent as View;

				if (Control == null)
				{
					RelativeLayout layout = new RelativeLayout(Context);
					RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, 1);

					_videoView = new VideoView(Context);
					_mediaController = new MediaController(Context);

					_videoView.SetOnPreparedListener(this);

					layout.AddView(_videoView);
					layoutParams.AddRule(LayoutRules.CenterInParent);

					_videoView.LayoutParameters = layoutParams;

					_mediaController.SetMediaPlayer(_videoView);
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
