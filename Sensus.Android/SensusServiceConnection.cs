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
    public class SensusServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public event EventHandler<ServiceConnectedEventArgs> ServiceConnected;

        private SensusServiceBinder _binder;

        public SensusServiceConnection(SensusServiceBinder binder)
        {
            if (binder != null)
                _binder = binder;
        }

        public void OnServiceConnected(ComponentName name, IBinder binder)
        {
            SensusServiceBinder serviceBinder = binder as SensusServiceBinder;

            if (serviceBinder != null)
            {
                _binder = serviceBinder;

                ServiceConnected(this, new ServiceConnectedEventArgs(serviceBinder));
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            _binder.Service = null;
        }
    }
}