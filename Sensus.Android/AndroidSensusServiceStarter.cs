using Android.App;
using Android.Content;
using Android.Widget;

namespace Sensus.Android
{
    /// <summary>
    /// Starts Sensus service on boot completion.
    /// </summary>
    [BroadcastReceiver]
    [IntentFilter(new string[] { Intent.ActionBootCompleted })]
    public class AndroidSensusServiceStarter : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Intent.ActionBootCompleted)
            {
                Toast.MakeText(context, "Starting Sensus Service", ToastLength.Long).Show();

                context.StartService(new Intent(context, typeof(AndroidSensusService)));
            }
        }
    }
}