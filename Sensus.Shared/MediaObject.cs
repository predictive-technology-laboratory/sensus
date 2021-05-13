
using Sensus.Probes.User.Scripts;
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static string GetFullCachePath(string cachePath)
		{
			return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), cachePath));
		}
		public static string GetCachePath(ScriptRunner scriptRunner, InputGroup inputGroup = null, Input input = null)
		{
			string path = Path.Combine(scriptRunner.Probe.Protocol.Id, $"{nameof(MediaObject)}Cache", scriptRunner.Script.Id);

			if (inputGroup != null)
			{
				path = Path.Combine(path, inputGroup.Id);
			}

			if (input != null)
			{
				path = Path.Combine(path, input.Id);
			}

			return path;
		}
		private static string GetCacheFileName(string cachePath)
		{
			return Path.Combine(cachePath, Guid.NewGuid().ToString());
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
			HttpClient client = SensusServiceHelper.HttpClient;

			using (HttpResponseMessage response = await client.GetAsync(url))
			{
				response.EnsureSuccessStatusCode();

				return new DownloadResult(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType.MediaType.ToLower());
			}
		}

		private static DownloadResult DownloadMedia(string url)
		{
			HttpClient client = SensusServiceHelper.HttpClient;

			using (HttpResponseMessage response = client.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult())
			{
				response.EnsureSuccessStatusCode();

				return new DownloadResult(response.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult(), response.Content.Headers.ContentType.MediaType.ToLower());
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
				return File.Exists(GetFullCachePath(CacheFileName));
			}
		}

		public async Task<Stream> WriteCacheFileAsync()
		{
			string fileName = GetFullCachePath(CacheFileName);

			DownloadResult result = await DownloadMediaAsync(Data);

			Directory.CreateDirectory(Path.GetDirectoryName(fileName));

			Stream stream = new FileStream(fileName, FileMode.Create);

			await stream.WriteAsync(result.Data);

			stream.Position = 0;

			return stream;
		}

		private void WriteCacheFile(byte[] buffer)
		{
			string fileName = GetFullCachePath(CacheFileName);

			Directory.CreateDirectory(Path.GetDirectoryName(fileName));

			Stream stream = new FileStream(fileName, FileMode.Create);

			stream.Write(buffer);

			stream.Position = 0;

			stream.Close();
		}
		public void WriteCacheFile()
		{
			DownloadResult result = DownloadMedia(Data);

			WriteCacheFile(result.Data);
		}

		public void ClearCache()
		{
			if (IsCached)
			{
				File.Delete(GetFullCachePath(CacheFileName));
			}
		}

		public static void ClearCache(ScriptRunner scriptRunner, InputGroup inputGroup = null, Input input = null)
		{
			string path = GetFullCachePath(GetCachePath(scriptRunner, inputGroup, input));

			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
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
					stream = new FileStream(GetFullCachePath(CacheFileName), FileMode.Open);
				}
				else
				{
					stream = await WriteCacheFileAsync();
				}
			}

			return stream;
		}

		public string GetMediaPath()
		{
			if (StorageMethod != MediaStorageMethods.URL)
			{
				return Data;
			}
			else
			{
				return GetFullCachePath(CacheFileName);
			}
		}

		private void CacheOnSerialization()
		{
			try
			{
				if (StorageMethod == MediaStorageMethods.Cache && IsCached == false)
				{
					WriteCacheFile();
				}
				else if (Type.ToLower().StartsWith("video") && StorageMethod == MediaStorageMethods.Embed)
				{
					WriteCacheFile(Convert.FromBase64String(Data));
				}
			}
			catch (Exception error)
			{
				SensusServiceHelper.Get().Logger.Log($"Failed to cache MediaInput media. Exception: {error.Message}", LoggingLevel.Normal, GetType());
			}
		}

		[OnSerialized]
		internal void OnSerializedMethod(StreamingContext context)
		{
			CacheOnSerialization();
		}

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			CacheOnSerialization();
		}
	}
}
