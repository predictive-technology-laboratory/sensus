using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Sensus
{
	public class VideoPlayer : View
	{
		public abstract class VideoSource
		{

		}

		public class FileSource : VideoSource
		{
			public FileSource(string path)
			{
				Path = path;
			}

			public string Path { get; set; }
		}

		public class UrlSource : VideoSource
		{
			public UrlSource(string url)
			{
				Url = url;
			}

			public string Url { get; set; }
		}

		public VideoSource Source { get; set; }
	}
}
