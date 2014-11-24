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

            _serviceHelper = new SensusServiceHelper();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _startId = startId;

            // the service can be stopped without destroying the service object. in such cases, 
            // subsequent calls to start the service will not call OnCreate, which is why the 
            // following code needs to run here. it's important that any code called here is
            // okay to call multiple times, even if the service is running. calling this when
            // the service is running can happen because sensus receives a signal on device
            // boot to start the service, and then when the sensus app is started the service
            // start method is called again.

            _serviceHelper.StartService();

            TaskStackBuilder stackBuilder = TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(MainActivity)));
            stackBuilder.AddNextIntent(new Intent(this, typeof(MainActivity)));

            PendingIntent pendingIntent = stackBuilder.GetPendingIntent(NotificationPendingIntentId, PendingIntentFlags.OneShot);

            _notificationBuilder = new Notification.Builder(this);
            _notificationBuilder.SetContentTitle("Sensus")
                                .SetContentText("Tap to Open")
                                .SetSmallIcon(Resource.Drawable.Icon)
                                .SetContentIntent(pendingIntent)
                                .SetAutoCancel(true)
                                .SetOngoing(true);

            _notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
            _notificationManager.Notify(ServiceNotificationId, _notificationBuilder.Build());

            return StartCommandResult.RedeliverIntent;
        }

        public void RegisterProtocol(Protocol protocol)
        {
            _serviceHelper.RegisterProtocol(protocol);
        }

        public void StartProtocol(Protocol protocol)
        {
            _serviceHelper.StartProtocol(protocol);
        }

        public void StopProtocol(Protocol protocol, bool unregister)
        {
            _serviceHelper.StopProtocol(protocol, unregister);
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
                {
                    _notificationManager.Cancel(ServiceNotificationId);
                    _serviceHelper.StopServiceAsync();
                    StopSelf();
                });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            _serviceHelper.StopServiceAsync();
            _serviceHelper.DestroyService();

            _notificationManager.Cancel(ServiceNotificationId);
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