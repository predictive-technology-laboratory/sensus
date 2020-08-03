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
			await DisposeMediaAsync();

			if (media != null)
			{
				if (media.Type.ToLower().StartsWith("image"))
				{
					if (media.Embeded)
					{
						_stream = new MemoryStream(Convert.FromBase64String(media.Data));
					}
					else
					{
						_stream = await GetFromUrlAsync(media);
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

					if (media.Embeded)
					{
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

		public /*async*/ Task DisposeMediaAsync()
		{
			if (_stream != null)
			{
				//await _stream.DisposeAsync();
				_stream.Dispose();

				_stream = null;
			}

			if (_filePath != null && File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}

			return Task.CompletedTask;
		}

		private async Task<Stream> GetFromUrlAsync(MediaObject media)
		{
			using (HttpClient client = new HttpClient())
			{
				using (HttpResponseMessage response = await client.GetAsync(media.Data))
				{
					return await response.Content.ReadAsStreamAsync();
				}
			}
		}

		public event EventHandler<VideoEventArgs> VideoEvent;
	}
}
