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
    /// Starts Sensus on boot completion.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionBootCompleted })]
    public class AndroidAppStarter : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Toast.MakeText(context, "Starting Sensus", ToastLength.Long).Show();

            if (intent.Action == Intent.ActionBootCompleted)
            {
                Intent applicationIntent = new Intent(context, typeof(MainActivity));
                applicationIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(applicationIntent);
            }
        }
    }
}