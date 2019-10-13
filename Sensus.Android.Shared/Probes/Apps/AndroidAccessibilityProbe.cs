using Android.App;
using Android.Content.PM;
using Android.Views.Accessibility;
using Sensus.Probes.Apps;
using System;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidAccessibilityProbe : AccessibilityProbe
	{
		protected async override Task StartListeningAsync()
		{
			await base.StartListeningAsync();

			await AndroidAccessibilityService.RegisterProbeAsync(this);
		}

		protected async override Task StopListeningAsync()
		{
			await base.StopListeningAsync();

			await AndroidAccessibilityService.UnregisterProbeAsync(this);
		}

		public async virtual Task OnAccessibilityEventAsync(AccessibilityEvent e)
		{
			string applicationName = Application.Context.PackageManager.GetApplicationLabel(Application.Context.PackageManager.GetApplicationInfo(e.PackageName, PackageInfoFlags.MatchDefaultOnly));

			AccessibilityDatum datum = new AccessibilityDatum(e.Enabled, e.CurrentItemIndex, e.Checked, e.ContentChangeTypes.ToString(), e.EventTime, e.EventType.ToString(), e.MovementGranularity.ToString(), e.FullScreen, e.ItemCount, e.PackageName, applicationName, e.ParcelableData, e.Password, e.RecordCount, e.FromIndex, e.AddedCount, e.Text.ToString(), e.ContentDescription, e.ClassName, e.BeforeText, e.GetAction().ToString(), e.RemovedCount, e.MaxScrollX, e.MaxScrollY, e.ScrollX, e.ScrollY, e.Scrollable, e.Source, e.ToIndex, e.WindowId, DateTimeOffset.UtcNow);

			await StoreDatumAsync(datum);
		}
	}
}
