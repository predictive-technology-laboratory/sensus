using Android.Views.Accessibility;
using Sensus.Probes.Apps;
using System;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidKeystrokeProbe : AndroidAccessibilityProbe
	{
		public override string DisplayName => "Keystroke";

		public override Type DatumType => typeof(KeystrokeDatum);

		public override async Task OnAccessibilityEventAsync(AccessibilityEvent e)
		{
			if (e.EventType == EventTypes.ViewTextChanged)
			{
				SensusServiceHelper.Get().Logger.Log($"Keystroke: {e.Text[0]}", LoggingLevel.Normal, GetType());

				await StoreDatumAsync(new KeystrokeDatum(DateTimeOffset.UtcNow, e.Text[0].ToString(), e.PackageName));
			}
		}
	}
}
