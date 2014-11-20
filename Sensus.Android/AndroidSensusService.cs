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
        private NotificationManager _notificationManager;
        private Notification.Builder _notificationBuilder;
        private const int ServiceNotificationId = 0;
        private const int NotificationPendingIntentId = 1;

        public IEnumerable<Protocol> RegisteredProtocols
        {
            get { return _serviceHelper.RegisteredProtocols; }
        }

        public LoggingLevel LoggingLevel
        {
            get { return _serviceHelper.Logger.Level; }
        }

        public AndroidSensusService()
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            _notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
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

                    _notificationManager.Notify(ServiceNotificationId, _notificationBuilder.Build());
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

        public Task StopAsync()
        {
            return Task.Run(async () =>
                {
                    await _serviceHelper.StopServiceAsync();

                    _notificationManager.Cancel(ServiceNotificationId);

                    StopSelf();
                });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            _notificationManager.Cancel(ServiceNotificationId);

            _serviceHelper.StopServiceAsync();
        }

        public override IBinder OnBind(Intent intent)
        {
            return new SensusServiceBinder(this);
        }        

        public void Log(string message)
        {
            _serviceHelper.Logger.WriteLine(message);
        }
    }
}