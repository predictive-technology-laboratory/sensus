
using Sensus.Probes.User.Scripts;
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
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
		public MediaCacheModes CacheMode { get; set; }

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
		public static string GetProtocolCachePath(ScriptRunner scriptRunner)
		{
			return Path.Combine(scriptRunner.Probe.Protocol.Id, $"{nameof(MediaObject)}Cache");
		}
		public static string GetCacheFileName(string fileName)
		{
			string extension = Path.GetExtension(fileName);
			string cacheName = null;

			using (MD5 sha = MD5.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fileName));
				Guid guid = new Guid(hash);

				cacheName = $"{guid}{extension}";
			}

			return cacheName;
		}
		public static string GetCacheFileName(string fileName, string cachePath)
		{
			return Path.Combine(cachePath, GetCacheFileName(fileName));
		}

		public static async Task<MediaObject> FromFileAsync(string path, Stream stream, string type, string cachePath)
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

			MediaObject media = new MediaObject(Convert.ToBase64String(fileBuffer), type, MediaStorageMethods.Embed, GetCacheFileName(path, cachePath));

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

			MediaObject media = new MediaObject(data, mimeType, storageMethod, GetCacheFileName(url, cachePath));

			return media;
		}

		public bool IsCached
		{
			get
			{
				return File.Exists(GetFullCachePath(CacheFileName));
			}
		}

		private Stream GetCacheFileStream()
		{
			string fileName = GetFullCachePath(CacheFileName);

			Directory.CreateDirectory(Path.GetDirectoryName(fileName));

			return new FileStream(fileName, FileMode.Create);
		}

		private async Task<Stream> WriteCacheFileAsync(byte[] buffer)
		{
			Stream stream = GetCacheFileStream();

			await stream.WriteAsync(buffer);

			stream.Position = 0;

			return stream;
		}
		public async Task<Stream> WriteCacheFileAsync()
		{
			DownloadResult result = await DownloadMediaAsync(Data);

			return await WriteCacheFileAsync(result.Data);
		}

		private void WriteCacheFile(byte[] buffer)
		{
			Stream stream = GetCacheFileStream();

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

		public static void ClearCache(ScriptRunner scriptRunner)
		{
			string path = GetFullCachePath(GetProtocolCachePath(scriptRunner));

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
				HttpClient client = SensusServiceHelper.HttpClient;

				using (HttpResponseMessage response = await client.GetAsync(Data))
				{
					stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
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
			if (StorageMethod == MediaStorageMethods.URL)
			{
				return Data;
			}
			else if (StorageMethod == MediaStorageMethods.Cache)
			{
				return GetFullCachePath(CacheFileName);
			}

			return null;
		}

		public async Task CacheMediaAsync()
		{
			try
			{
				if (StorageMethod == MediaStorageMethods.Cache && IsCached == false)
				{
					await WriteCacheFileAsync();
				}
				else if (Type.ToLower().StartsWith("video") && StorageMethod == MediaStorageMethods.Embed)
				{
					await WriteCacheFileAsync(Convert.FromBase64String(Data));
				}
			}
			catch (Exception error)
			{
				SensusServiceHelper.Get().Logger.Log($"Failed to cache MediaInput media. Exception: {error.Message}", LoggingLevel.Normal, GetType());
			}
		}

		private void CacheMedia()
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
			if (CacheMode.HasFlag(MediaCacheModes.OnSerialization))
			{
				CacheMedia();
			}
		}

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			if (CacheMode.HasFlag(MediaCacheModes.OnDeserialization))
			{
				CacheMedia();
			}
		}
	}
}
