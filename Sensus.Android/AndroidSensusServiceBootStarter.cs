using Android.App;
using Android.Content;
using Android.Widget;
using SensusService;
using System;

namespace Sensus.Android
{
    /// <summary>
    /// Starts Sensus service on boot completion.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionBootCompleted }, Categories = new string[] { Intent.CategoryDefault })]
    public class AndroidSensusServiceBootStarter : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Intent.ActionBootCompleted)
                context.StartService(new Intent(context, typeof(AndroidSensusService)));
        }
    }
}