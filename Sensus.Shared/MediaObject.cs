
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Sensus
{
	public class MediaObject
	{
		public string Data { get; private set; }
		public string Type { get; private set; }
		public MediaStorageMethods StorageMethod { get; private set; }
		public string CacheFileName { get; private set; }

		// for deserialization and manual construction
		public MediaObject(string data, string type, MediaStorageMethods storageMethod, string cacheFileName)
		{
			Data = data;
			Type = type;
			StorageMethod = storageMethod;
			CacheFileName = cacheFileName;
		}

		private static string GetCacheFileName(string cachePath)
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), cachePath, "MediaInputCache", Guid.NewGuid().ToString());
		}

		public static async Task<MediaObject> FromFileAsync(Stream stream, string type, string cachePath)
		{
			const int BUFFER_SIZE = 1024;
			byte[] fileBuffer = new byte[stream.Length];
			byte[] buffer = new byte[BUFFER_SIZE];
			int totalBytesRead = 0;
			int bytesRead;

			while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				Array.Copy(buffer, 0, fileBuffer, totalBytesRead, bytesRead);

				totalBytesRead += bytesRead;
			}

			MediaObject media = new MediaObject(Convert.ToBase64String(fileBuffer), type, MediaStorageMethods.Embed, GetCacheFileName(cachePath));

			return media;
		}

		private readonly struct DownloadResult
		{
			public DownloadResult(byte[] data, string mimeType)
			{
				Data = data;
				MimeType = mimeType;
			}

			public byte[] Data { get; }
			public string MimeType { get; } 

		}

		private static async Task<DownloadResult> DownloadMediaAsync(string url)
		{
			using (HttpClient client = new HttpClient())
			{
				using (HttpResponseMessage response = await client.GetAsync(url))
				{
					response.EnsureSuccessStatusCode();

					return new DownloadResult(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType.MediaType.ToLower());
				}
			}
		}

		public static async Task<MediaObject> FromUrlAsync(string url, string mimeType, MediaStorageMethods storageMethod, string cachePath)
		{
			string data = url;

			if (storageMethod == MediaStorageMethods.Embed)
			{
				DownloadResult result = await DownloadMediaAsync(url);

				if (string.IsNullOrEmpty(mimeType))
				{
					mimeType = result.MimeType;
				}

				data = Convert.ToBase64String(result.Data);
			}

			MediaObject media = new MediaObject(data, mimeType, storageMethod, GetCacheFileName(cachePath));

			return media;
		}

		public bool IsCached
		{
			get
			{
				return File.Exists(CacheFileName);
			}
		}

		private async Task<Stream> WriteCacheFile(byte[] buffer)
		{
			Stream stream = new FileStream(CacheFileName, FileMode.Create);

			await stream.WriteAsync(buffer);

			stream.Position = 0;

			return stream;
		}
		private async Task<Stream> WriteCacheFile()
		{
			DownloadResult result = await DownloadMediaAsync(Data);

			return await WriteCacheFile(result.Data);
		}

		public async Task<Stream> GetMediaStreamAsync()
		{
			Stream stream = new MemoryStream();

			if (StorageMethod == MediaStorageMethods.Embed)
			{
				stream = new MemoryStream(Convert.FromBase64String(Data));
			}
			else if (StorageMethod == MediaStorageMethods.URL)
			{
				using (HttpClient client = new HttpClient())
				{
					using (HttpResponseMessage response = await client.GetAsync(Data))
					{
						stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
					}
				}
			}
			else if (StorageMethod == MediaStorageMethods.Cache)
			{
				if (IsCached)
				{
					stream = new FileStream(CacheFileName, FileMode.Open);
				}
				else
				{
					stream = await WriteCacheFile();
				}
			}

			return stream;
		}

		[OnSerializing]
		[OnDeserializing]
		internal async Task OnSerializingMethod(StreamingContext context)
		{
			if ((StorageMethod == MediaStorageMethods.Cache && IsCached == false))
			{
				await WriteCacheFile();
			}
			else if (Type.ToLower().StartsWith("video") && StorageMethod == MediaStorageMethods.Embed)
			{
				await WriteCacheFile(Convert.FromBase64String(Data));
			}
		}
	}
}
