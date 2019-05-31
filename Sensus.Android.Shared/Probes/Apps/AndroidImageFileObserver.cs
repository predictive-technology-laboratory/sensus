using Android.OS;
using Android.Provider;
using Android.Runtime;
using System.IO;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidImageFileObserver : FileObserver
	{
		private string _path;

		public AndroidImageFileObserver(string path) : base(path, FileObserverEvents.CloseWrite)
		{
			_path = path;
		}

		public override void StartWatching()
		{
			if (File.Exists(_path))
			{
				return;
			}

			base.StartWatching();
		}

		public override void OnEvent([GeneratedEnum] FileObserverEvents e, string path)
		{
			
		}
	}
}
