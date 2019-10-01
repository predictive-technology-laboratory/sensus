using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading.Tasks;
using ExifLib;
using Sensus.UI.UiProperties;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	public abstract class ImageMetadataProbe : ListeningProbe
	{
		public const string IMAGE_DISCRETE_TYPE = "image/";
		public const string VIDEO_DISCRETE_TYPE = "video/";
		public const string JPEG_MIME_TYPE = "image/jpeg";

		private bool _storeImages;
		private bool _storeVideos;

		protected override bool DefaultKeepDeviceAwake
		{
			get
			{
				return false;
			}
		}

		public override Type DatumType
		{
			get
			{
				return typeof(ImageMetadataDatum);
			}
		}

		public override string DisplayName
		{
			get
			{
				return "Gallery Image Metadata";
			}
		}

		protected override string DeviceAwakeWarning
		{
			get
			{
				return "";
			}
		}

		protected override string DeviceAsleepWarning
		{
			get
			{
				return "";
			}
		}

		[OnOffUiProperty("Store images:", true, 4)]
		public bool StoreImages
		{
			get
			{
				return _storeImages;
			}
			set
			{
				_storeImages = value;
			}
		}
		[OnOffUiProperty("Store videos:", true, 5)]
		public bool StoreVideos
		{
			get
			{
				return _storeVideos;
			}
			set
			{
				_storeVideos = value;
			}
		}

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			throw new NotImplementedException();
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override ChartSeries GetChartSeries()
		{
			throw new NotImplementedException();
		}

		public async Task<byte[]> ReadFile(FileStream fileStream)
		{
			const int BUFFER_SIZE = 1024;
			byte[] fileBuffer = new byte[fileStream.Length];
			byte[] buffer = new byte[BUFFER_SIZE];
			int totalBytesRead = 0;
			int bytesRead = 0;

			while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				Array.Copy(buffer, 0, fileBuffer, totalBytesRead, bytesRead);

				totalBytesRead += bytesRead;
			}

			return fileBuffer;
		}

		public async Task CreateAndStoreDatumAsync(string path, string mimeType, DateTime timestamp)
		{
			if (File.Exists(path))
			{
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					string imageBase64 = null;

					if (mimeType == JPEG_MIME_TYPE)
					{
						JpegInfo info = ExifReader.ReadJpeg(fs);

						double latitude = 0;
						double longitude = 0;

						if (info.GpsLatitude != null && info.GpsLongitude != null)
						{
							latitude = (Math.Truncate(info.GpsLatitude[0]) + (info.GpsLatitude[1] / 60) + (info.GpsLatitude[2] / 3600)) * (info.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
							longitude = (Math.Truncate(info.GpsLongitude[0]) + (info.GpsLongitude[1] / 60) + (info.GpsLongitude[2] / 3600)) * (info.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
						}

						if (StoreImages)
						{
							fs.Position = 0;

							imageBase64 = Convert.ToBase64String(await ReadFile(fs));
						}

						await StoreDatumAsync(new ImageMetadataDatum(info.FileSize, info.Width, info.Height, (int)info.Orientation, info.XResolution, info.YResolution, (int)info.ResolutionUnit, info.IsColor, (int)info.Flash, info.FNumber, info.ExposureTime, info.Software, latitude, longitude, mimeType, imageBase64, timestamp));
					}
					else // the file is something else...
					{
						if (StoreVideos || (StoreImages && mimeType.StartsWith(IMAGE_DISCRETE_TYPE)))
						{
							fs.Position = 0;

							imageBase64 = Convert.ToBase64String(await ReadFile(fs));
						}

						await StoreDatumAsync(new ImageMetadataDatum((int)fs.Length, null, null, null, null, null, null, null, null, null, null, null, null, null, mimeType, imageBase64, timestamp));
					}
				}
			}
		}
	}
}
