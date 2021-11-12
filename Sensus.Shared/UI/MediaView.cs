using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus.UI
{
	public class MediaView : ContentView
	{
		private Stream _stream;

		public MediaView()
		{

		}

		public async Task SetMediaAsync(MediaObject media)
		{
			try
			{
				await DisposeMediaAsync();

				if (media != null)
				{
					if (media.Type.ToLower().StartsWith("image"))
					{
						_stream = await media.GetMediaStreamAsync();

						Content = new Image
						{
							Source = ImageSource.FromStream(() => _stream)
						};
					}
					else if (media.Type.ToLower().StartsWith("video"))
					{
						VideoPlayer player = new VideoPlayer();

						player.VideoEvent += VideoEvent;

						if (media.StorageMethod == MediaStorageMethods.URL)
						{
							player.Source = new VideoPlayer.UrlSource(media.GetMediaPath());
						}
						else if (media.StorageMethod == MediaStorageMethods.Cache)
						{
							player.Source = new VideoPlayer.FileSource(media.GetMediaPath());
						}

						Content = player;
					}
				}
			}
			catch (Exception error)
			{
				SensusServiceHelper.Get().Logger.Log($"Unable to set MediaView media object. Exception: {error.Message}", LoggingLevel.Normal, GetType());
			}
		}

		public async Task DisposeMediaAsync()
		{
			if (_stream != null)
			{
				await _stream.DisposeAsync();

				_stream = null;
			}
		}

		public event EventHandler<VideoEventArgs> VideoEvent;
	}
}
