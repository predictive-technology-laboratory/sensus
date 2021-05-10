using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sensus
{
	public class MediaView : ContentView
	{
		private Stream _stream;
		private string _filePath;

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
						if (media.StorageMethod == MediaStorageMethods.Embed)
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
						}

						Content = new Image
						{
							Source = ImageSource.FromStream(() => _stream)
						};
					}
					else if (media.Type.ToLower().StartsWith("video"))
					{
						VideoPlayer player = new VideoPlayer();

						player.VideoEvent += VideoEvent;

						if (media.StorageMethod == MediaStorageMethods.Embed)
						{
							// it should be possible to optimize this to use the same temp file name each time the video is loaded 
							// and only copy it to the temp file if it doesn't exist or its creation time is a certain distance in 
							// the past.
							_filePath = Path.GetTempFileName();

							_stream = new FileStream(_filePath, FileMode.Open);

							using (BinaryWriter writer = new BinaryWriter(_stream))
							{
								writer.Write(Convert.FromBase64String(media.Data));
							}

							player.Source = new VideoPlayer.FileSource(_filePath);
						}
						else
						{
							player.Source = new VideoPlayer.UrlSource(media.Data);
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

			if (_filePath != null && File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}
		}

		public event EventHandler<VideoEventArgs> VideoEvent;
	}
}
