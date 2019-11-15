using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Sensus.Android.UI;
using Sensus;
using RelativeLayout = Android.Widget.RelativeLayout;
using Uri = Android.Net.Uri;

[assembly: ExportRenderer(typeof(VideoPlayer), typeof(AndroidVideoRenderer))]

namespace Sensus.Android.UI
{
	public class AndroidVideoRenderer : ViewRenderer<VideoPlayer, RelativeLayout>
	{
		private VideoView _videoView;
		private MediaController _mediaController;

		public AndroidVideoRenderer(global::Android.Content.Context context) : base(context)
		{

		}

		protected override void OnElementChanged(ElementChangedEventArgs<VideoPlayer> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					RelativeLayout layout = new RelativeLayout(Context);
					RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
					
					_videoView = new VideoView(Context);
					_mediaController = new MediaController(Context);
					
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
