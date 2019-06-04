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
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Apps
{
	public class ImageMetadataDatum : Datum
	{
		public override string DisplayDetail => throw new NotImplementedException();

		public override object StringPlaceholderValue => throw new NotImplementedException();

		public ImageMetadataDatum(int fileSize, int width, int height, int orientation, double xResolution, double yResolution, int resolutionUnit, bool isColor, int flash, double fNumber, double exposureTime, string software, double? latitude, double? longitude, string imageBase64, DateTimeOffset timestamp)
		{
			FileSize = fileSize;
			Width = width;
			Height = height;
			Orientation = orientation;
			XResolution = xResolution;
			YResolution = yResolution;
			ResolutionUnit = resolutionUnit;
			IsColor = isColor;
			Flash = flash;
			FNumber = fNumber;
			ExposureTime = exposureTime;
			Software = software;
			Latitude = latitude;
			Longitude = longitude;
			ImageBase64 = imageBase64;
			Timestamp = timestamp;
		}

		public int FileSize { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int Orientation { get; set; }
		public double XResolution { get; set; }
		public double YResolution { get; set; }
		public int ResolutionUnit { get; set; }
		public bool IsColor { get; set; }
		public int Flash { get; set; }
		public double FNumber { get; set; }
		public double ExposureTime { get; set; }
		public string Software { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public string ImageBase64 { get; set; }

		//public int ThumbnailSize { get; set; }
		//public int ThumbnailOffset { get; set; }
		//public ExifGpsLongitudeRef GpsLongitudeRef { get; set; }
		//public ExifGpsLatitudeRef GpsLatitudeRef { get; set; }
		//public ExifFlash Flash { get; set; }
		//public double FNumber { get; set; }
		//public double ExposureTime { get; set; }
		//public string UserComment { get; set; }
		//public string Copyright { get; set; }
		//public string Artist { get; set; }
		//public string Software { get; set; }
		//public string Model { get; set; }
		//public string Make { get; set; }
		//public string Description { get; set; }
		//public string DateTimeOriginal { get; set; }
		//public string DateTime { get; set; }
		//public ExifUnit ResolutionUnit { get; set; }
		//public double YResolution { get; set; }
		//public double XResolution { get; set; }
		//public ExifOrientation Orientation { get; set; }
		//public bool IsColor { get; set; }
		//public int Width { get; set; }
		//public int Height { get; set; }
		//public bool IsValid { get; set; }
		//public int FileSize { get; set; }
		//public string FileName { get; set; }
		//public byte[] ThumbnailData { get; set; }
		//public TimeSpan LoadTime { get; set; }
	}
}
