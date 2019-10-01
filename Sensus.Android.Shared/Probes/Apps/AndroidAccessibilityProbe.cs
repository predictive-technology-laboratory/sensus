using Android.Views.Accessibility;
using Sensus.Probes;
using Sensus.Probes.Apps;
using Syncfusion.SfChart.XForms;
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
			AccessibilityDatum datum = new AccessibilityDatum(e.Enabled, e.CurrentItemIndex, e.Checked, e.ContentChangeTypes.ToString(), e.EventTime, e.EventType.ToString(), e.MovementGranularity.ToString(), e.FullScreen, e.ItemCount, e.PackageName, e.ParcelableData, e.Password, e.RecordCount, e.FromIndex, e.AddedCount, e.Text.ToString(), e.ContentDescription, e.ClassName, e.BeforeText, e.GetAction().ToString(), e.WindowChanges.ToString(), e.RemovedCount, e.MaxScrollX, e.MaxScrollY, e.ScrollDeltaY, e.ScrollX, e.ScrollY, e.Scrollable, e.Source, e.ScrollDeltaX, e.ToIndex, e.WindowId, DateTimeOffset.FromUnixTimeMilliseconds(e.EventTime));

			await StoreDatumAsync(datum);
		}
	}
}
