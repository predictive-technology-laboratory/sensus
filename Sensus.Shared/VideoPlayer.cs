using System;
using Xamarin.Forms;

namespace Sensus
{
	public class VideoEventArgs
	{
		public VideoEventArgs(string @event, TimeSpan position)
		{
			Event = @event;
			Position = position;
		}

		public string Event { get; set; }
		public TimeSpan Position { get; set; }
	}

	public class VideoPlayer : View
	{
		public const string START = "Start";
		public const string SEEK = "Seek";
		public const string PAUSE = "Pause";
		public const string RESUME = "Resume";
		public const string END = "End";

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

		public event EventHandler<VideoEventArgs> VideoEvent;

		public void OnVideoEvent(VideoEventArgs e)
		{
			VideoEvent?.Invoke(this, e);
		}

		public event EventHandler<VideoEventArgs> VideoStart;

		public void OnVideoStart(VideoEventArgs e)
		{
			VideoStart?.Invoke(this, e);
			OnVideoEvent(e);
		}

		public event EventHandler<VideoEventArgs> VideoSeek;

		public void OnVideoSeek(VideoEventArgs e)
		{
			VideoSeek?.Invoke(this, e);
			OnVideoEvent(e);
		}

		public event EventHandler<VideoEventArgs> VideoPause;

		public void OnVideoPause(VideoEventArgs e)
		{
			VideoPause?.Invoke(this, e);
			OnVideoEvent(e);
		}

		public event EventHandler<VideoEventArgs> VideoResume;

		public void OnVideoResume(VideoEventArgs e)
		{
			VideoResume?.Invoke(this, e);
			OnVideoEvent(e);
		}

		public event EventHandler<VideoEventArgs> VideoEnd;

		public void OnVideoEnd(VideoEventArgs e)
		{
			VideoEnd?.Invoke(this, e);
			OnVideoEvent(e);
		}
	}
}
