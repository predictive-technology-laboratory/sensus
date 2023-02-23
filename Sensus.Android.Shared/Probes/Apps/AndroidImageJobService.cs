using Android.App;
using Android.App.Job;
using System.Threading.Tasks;
using Uri = Android.Net.Uri;

namespace Sensus.Android.Probes.Apps
{
	[Service(Name = "com.sensus.android.AndroidImageJobService", Permission = "android.permission.BIND_JOB_SERVICE")]
	public class AndroidImageJobService : JobService
	{
		public override bool OnStartJob(JobParameters parameters)
		{
			AndroidImageMetadataProbe.ScheduleJob();

			Task.Run(async () =>
			{
				Uri[] uris = parameters.GetTriggeredContentUris() ?? new Uri[0];

				foreach (Uri uri in uris)
				{
					await AndroidImageMetadataProbe.CreateDatumAsync(uri);

					JobFinished(parameters, false);
				}
			});

			return true;
		}

		public override bool OnStopJob(JobParameters parameters)
		{
			return true;
		}
	}
}
