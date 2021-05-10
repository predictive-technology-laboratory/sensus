
using System;
using System.IO;
using System.Net.Http;
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
		public MediaObject(string data, string type, MediaStorageMethods storageMethod)
		{
			Data = data;
			Type = type;
			StorageMethod = storageMethod;
		}

		public static async Task<MediaObject> FromFileAsync(Stream stream, string type)
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

			MediaObject media = new MediaObject(Convert.ToBase64String(fileBuffer), type, MediaStorageMethods.Embed);

			return media;
		}

		public static async Task<MediaObject> FromUrlAsync(string url, string mimeType, MediaStorageMethods storageMethod)
		{
			string data = url;

			if (storageMethod != MediaStorageMethods.URL)
			{
				using (HttpClient client = new HttpClient())
				{
					using (HttpResponseMessage response = await client.GetAsync(url))
					{
						response.EnsureSuccessStatusCode();

						if (string.IsNullOrEmpty(mimeType))
						{
							mimeType = response.Content.Headers.ContentType.MediaType.ToLower();
						}

						data = Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync());
					}
				}
			}

			MediaObject media = new MediaObject(data, mimeType, storageMethod);

			return media;
		}

		public bool IsCached
		{
			get
			{
				return File.Exists(CacheFileName);
			}
		}
	}
}
