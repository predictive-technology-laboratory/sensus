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

using Android;
using Android.App;
//using Android.Content.PM;
using Android.Database;
using Android.Provider;
using Android.Support.V4.Content;
using Newtonsoft.Json;
using Plugin.Permissions.Abstractions;
using Sensus.Probes.Apps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageMetadataProbe : ImageMetadataProbe
	{
		[JsonIgnore]
		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		protected async override Task InitializeAsync()
		{
			await base.InitializeAsync();
		}

		protected async override Task<List<ImageMetadataDatum>> GetImages()
		{
			List<ImageMetadataDatum> imageMetadata = new List<ImageMetadataDatum>();

			//if (ContextCompat.CheckSelfPermission(Application.Context, Manifest.Permission.ReadExternalStorage) == Permission.Granted)
			//{
			if (await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Storage) == PermissionStatus.Granted)
			{
				string[] properties = { MediaStore.Images.Media.InterfaceConsts.Id, MediaStore.Images.Media.InterfaceConsts.DateTaken };

				global::Android.Net.Uri uri = MediaStore.Images.Media.ExternalContentUri;

				ICursor cursor = Application.Context.ContentResolver.Query(uri, properties, MediaStore.Images.Media.InterfaceConsts.DateTaken + $" >=?", new string[] { (Java.Lang.JavaSystem.CurrentTimeMillis() - PollingSleepDurationMS).ToString() }, MediaStore.Images.Media.InterfaceConsts.DateTaken + " DESC");
				List<(string, string)> photos = new List<(string, string)>();
				while (cursor.MoveToNext())
				{
					photos.Add((cursor.GetString(0), cursor.GetString(1)));
				}
			}

			return imageMetadata;
		}
	}
}
