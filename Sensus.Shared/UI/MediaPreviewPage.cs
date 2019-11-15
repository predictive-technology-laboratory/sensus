using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class MediaPreviewPage : ContentPage
	{
		public MediaPreviewPage(MediaObject media)
		{
			Title = "Preview";

			MediaView mediaView = new MediaView();

			Content = mediaView;

			Appearing += async (s, e) =>
			{
				await mediaView.SetMediaAsync(media);
			};

			Disappearing += async (s, e) =>
			{
				await mediaView.DisposeMediaAsync();
			};
		}
	}
}
