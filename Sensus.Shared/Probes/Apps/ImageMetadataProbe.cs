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

using System;
using System.IO;
using System.Threading.Tasks;
using ExifLib;
using Sensus.UI.UiProperties;
using Syncfusion.SfChart.XForms;

namespace Sensus.Probes.Apps
{
	/// <summary>
	/// Collects application usage stats as <see cref="ImageMetadataDatum"/>
	/// </summary>
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

		protected static void SetExifData(Stream stream, ref ImageMetadataDatum datum)
		{
			try
			{
				stream.Position = 0;

				JpegInfo exif = ExifReader.ReadJpeg(stream);

				double latitude = 0;
				double longitude = 0;

				if (datum.FileSize == 0)
				{
					datum.FileSize = exif.FileSize;
				}

				datum.Width ??= exif.Width;
				datum.Height ??= exif.Height;
				datum.Orientation = (int)exif.Orientation;
				datum.XResolution = exif.XResolution;
				datum.YResolution = exif.YResolution;
				datum.ResolutionUnit = (int)exif.ResolutionUnit;
				datum.IsColor = exif.IsColor;
				datum.Flash = (int)exif.Flash;
				datum.FNumber = exif.FNumber;
				datum.ExposureTime = exif.ExposureTime;
				datum.Software = exif.Software;

				if (exif.GpsLatitude != null && exif.GpsLongitude != null)
				{
					latitude = (Math.Truncate(exif.GpsLatitude[0]) + (exif.GpsLatitude[1] / 60) + (exif.GpsLatitude[2] / 3600)) * (exif.GpsLatitudeRef == ExifGpsLatitudeRef.North ? 1 : -1);
					longitude = (Math.Truncate(exif.GpsLongitude[0]) + (exif.GpsLongitude[1] / 60) + (exif.GpsLongitude[2] / 3600)) * (exif.GpsLongitudeRef == ExifGpsLongitudeRef.East ? 1 : -1);
				}

				datum.Latitude = latitude;
				datum.Longitude = longitude;
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get().Logger.Log($"Exception while reading EXIF data: {e.Message}", LoggingLevel.Normal, typeof(ImageMetadataProbe));
			}
		}

		protected static async Task<string> GetImageBase64(Stream stream)
		{
			try
			{
				stream.Position = 0;

				byte[] bytes = await SensusServiceHelper.ReadAllBytesAsync(stream);

				return Convert.ToBase64String(bytes);
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get().Logger.Log($"Exception while converting media to base64: {e.Message}", LoggingLevel.Normal, typeof(ImageMetadataProbe));
			}

			return null;
		}
	}
}
