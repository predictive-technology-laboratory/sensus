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

using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using System;

namespace Sensus.Probes.Apps
{
	public class ImageMetadataDatum : Datum
	{
		public override string DisplayDetail
        {
            get
            {
                return "(Image Metadata)";
            }
        }

        public override object StringPlaceholderValue => throw new NotImplementedException();

		public ImageMetadataDatum(int fileSize, int? width, int? height, int? orientation, double? xResolution, double? yResolution, int? resolutionUnit, bool? isColor, int? flash, double? fNumber, double? exposureTime, string software, double? latitude, double? longitude, string mimeType, string imageBase64, DateTimeOffset timestamp) : base(timestamp)
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
			MimeType = mimeType;
			ImageBase64 = imageBase64;
		}

		public int FileSize { get; set; }
		public int? Width { get; set; }
		public int? Height { get; set; }
		public int? Orientation { get; set; }
		public double? XResolution { get; set; }
		public double? YResolution { get; set; }
		public int? ResolutionUnit { get; set; }
		public bool? IsColor { get; set; }
		public int? Flash { get; set; }
		public double? FNumber { get; set; }
		public double? ExposureTime { get; set; }
		[Anonymizable("Software:", typeof(StringHashAnonymizer), false)]
		public string Software { get; set; }
		[Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
		public double? Latitude { get; set; }
		[Anonymizable(null, new Type[] { typeof(LongitudeParticipantOffsetGpsAnonymizer), typeof(LongitudeStudyOffsetGpsAnonymizer), typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
		public double? Longitude { get; set; }
		public string MimeType { get; set; }
		[Anonymizable("Image:", typeof(StringHashAnonymizer), false)]
		public string ImageBase64 { get; set; }
	}
}
