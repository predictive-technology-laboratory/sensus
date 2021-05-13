using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus
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
						/*if (media.StorageMethod == MediaStorageMethods.Embed)
						{
							_stream = new MemoryStream(Convert.FromBase64String(media.Data));
						}
						else
						{
							bool createCache = File.Exists(media.CacheFileName) == false;

							if (media.StorageMethod == MediaStorageMethods.URL || createCache)
							{
								using (HttpClient client = new HttpClient())
								{
									using (HttpResponseMessage response = await client.GetAsync(media.Data))
									{
										_stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
									}
								}

								if (createCache)
								{
									Stream stream = _stream;

									media.CacheFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), Guid.NewGuid().ToString());

									_stream = new FileStream(media.CacheFileName, FileMode.Create);

									stream.CopyTo(_stream);

									_stream.Position = 0;

									stream.Dispose();
								}
								else
								{
									_stream = new FileStream(media.CacheFileName, FileMode.Open);
								}
							}
						}*/

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

						if (media.StorageMethod != MediaStorageMethods.URL)
						{
							player.Source = new VideoPlayer.UrlSource(media.GetMediaPath());
						}
						else
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
