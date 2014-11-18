using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Sensus.Android
{
    /// <summary>
    /// Starts Sensus on device boot completion.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionBootCompleted }, Categories = new string[] { Intent.CategoryDefault })]
    public class SensusServiceStarter : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Toast.MakeText(context, "Starting Sensus", ToastLength.Short).Show();

            if ((intent.Action != null) && (intent.Action == Intent.ActionBootCompleted))
                context.ApplicationContext.StartService(new Intent(context, typeof(AndroidSensusService)));
        }
    }
}