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
using System.Threading.Tasks;

namespace Sensus.Android
{
    [Service]
    public class AndroidSensusService : Service, ISensusService
    {
        private int _startId;
        private SensusServiceHelper _serviceHelper;
        private Notification.Builder _notificationBuilder;
        private const int ServiceNotificationId = 0;
        private const int NotificationPendingIntentId = 1;

        public Logger Logger
        {
            get { return _serviceHelper.Logger; }
        }

        public IEnumerable<Protocol> StartedProtocols
        {
            get { return _serviceHelper.StartedProtocols; }
        }

        public AndroidSensusService()
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            _notificationBuilder = new Notification.Builder(this);
            _serviceHelper = new SensusServiceHelper();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _startId = startId;

            Task.Run(() =>
                {
                    TaskStackBuilder stackBuilder = TaskStackBuilder.Create(this);
                    stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(MainActivity)));
                    stackBuilder.AddNextIntent(new Intent(this, typeof(MainActivity)));

                    PendingIntent pendingIntent = stackBuilder.GetPendingIntent(NotificationPendingIntentId, PendingIntentFlags.OneShot);

                    _notificationBuilder.SetContentTitle("Sensus")
                                        .SetContentText("Tap to Open")
                                        .SetSmallIcon(Resource.Drawable.Icon)
                                        .SetContentIntent(pendingIntent)
                                        .SetAutoCancel(true)
                                        .SetOngoing(true);

                    NotificationManager notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;

                    notificationManager.Notify(ServiceNotificationId, _notificationBuilder.Build());
                });

            return StartCommandResult.RedeliverIntent;
        }

        public void StartProtocol(Protocol protocol)
        {
            _serviceHelper.StartProtocol(protocol);
        }

        public void StopProtocol(Protocol protocol)
        {
            _serviceHelper.StopProtocol(protocol);
        }

        public void Stop()
        {
            _serviceHelper.Stop();

            if (StopSelfResult(_startId))
                if (Logger.Level >= LoggingLevel.Normal)
                    Logger.Log("Stopped Sensus service.");
        }

        public override void OnDestroy()
        {
            if (Logger.Level >= LoggingLevel.Normal)
                Logger.Log("Destroying Sensus service.");

            base.OnDestroy();

            _serviceHelper.Stop();
        }

        public override IBinder OnBind(Intent intent)
        {
            return new SensusServiceBinder(this);
        }
    }
}