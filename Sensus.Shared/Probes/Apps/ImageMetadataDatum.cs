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
		private int _fileSize;
		private int? _width;
		private int? _height;
		private int? _orientation;
		private double? _xResolution;
		private double? _yResolution;
		private int? _resolutionUnit;
		private bool? _isColor;
		private int? _flash;
		private double? _fNumber;
		private double? _exposureTime;
		private string _software;
		private double? _latitude;
		private double? _longitude;
		private string _mimeType;
		private string _imageBase64;

		public override string DisplayDetail
		{
			get
			{
				return "(Image Metadata)";
			}
		}

		public override object StringPlaceholderValue
		{
			get
			{
				return "(Image Metadata)";
			}
		}

		public ImageMetadataDatum(int fileSize, int? width, int? height, int? orientation, double? xResolution, double? yResolution, int? resolutionUnit, bool? isColor, int? flash, double? fNumber, double? exposureTime, string software, double? latitude, double? longitude, string mimeType, string imageBase64, DateTimeOffset timestamp) : base(timestamp)
		{
			_fileSize = fileSize;
			_width = width;
			_height = height;
			_orientation = orientation;
			_xResolution = xResolution;
			_yResolution = yResolution;
			_resolutionUnit = resolutionUnit;
			_isColor = isColor;
			_flash = flash;
			_fNumber = fNumber;
			_exposureTime = exposureTime;
			_software = software;
			_latitude = latitude;
			_longitude = longitude;
			_mimeType = mimeType;
			_imageBase64 = imageBase64;
		}

		public int FileSize
		{
			get
			{
				return _fileSize;
			}
			set
			{
				_fileSize = value;
			}
		}
		public int? Width
		{
			get
			{
				return _width;
			}
			set
			{
				_width = value;
			}
		}
		public int? Height
		{
			get
			{
				return _height;
			}
			set
			{
				_height = value;
			}
		}
		public int? Orientation
		{
			get
			{
				return _orientation;
			}
			set
			{
				_orientation = value;
			}
		}
		public double? XResolution
		{
			get
			{
				return _xResolution;
			}
			set
			{
				_xResolution = value;
			}
		}
		public double? YResolution
		{
			get
			{
				return _yResolution;
			}
			set
			{
				_yResolution = value;
			}
		}
		public int? ResolutionUnit
		{
			get
			{
				return _resolutionUnit;
			}
			set
			{
				_resolutionUnit = value;
			}
		}
		public bool? IsColor
		{
			get
			{
				return _isColor;
			}
			set
			{
				_isColor = value;
			}
		}
		public int? Flash
		{
			get
			{
				return _flash;
			}
			set
			{
				_flash = value;
			}
		}
		public double? FNumber
		{
			get
			{
				return _fNumber;
			}
			set
			{
				_fNumber = value;
			}
		}
		public double? ExposureTime
		{
			get
			{
				return _exposureTime;
			}
			set
			{
				_exposureTime = value;
			}
		}
		[Anonymizable("Software:", typeof(StringHashAnonymizer), false)]
		public string Software
		{
			get
			{
				return _software;
			}
			set
			{
				_software = value;
			}
		}
		[Anonymizable(null, new Type[] { typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
		public double? Latitude
		{
			get
			{
				return _latitude;
			}
			set
			{
				_latitude = value;
			}
		}
		[Anonymizable(null, new Type[] { typeof(LongitudeParticipantOffsetGpsAnonymizer), typeof(LongitudeStudyOffsetGpsAnonymizer), typeof(DoubleRoundingTenthsAnonymizer), typeof(DoubleRoundingHundredthsAnonymizer), typeof(DoubleRoundingThousandthsAnonymizer) }, -1)]
		public double? Longitude
		{
			get
			{
				return _longitude;
			}
			set
			{
				_longitude = value;
			}
		}
		public string MimeType
		{
			get
			{
				return _mimeType;
			}
			set
			{
				_mimeType = value;
			}
		}
		[Anonymizable("Image:", typeof(StringHashAnonymizer), false)]
		public string ImageBase64
		{
			get
			{
				return _imageBase64;
			}
			set
			{
				_imageBase64 = value;
			}
		}
	}
}
