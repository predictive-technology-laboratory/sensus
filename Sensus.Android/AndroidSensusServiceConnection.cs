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
    public class AndroidSensusServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public event EventHandler<AndroidServiceConnectedEventArgs> ServiceConnected;

        private AndroidSensusServiceBinder _binder;

        public AndroidSensusServiceBinder Binder
        {
            get { return _binder; }
        }

        public AndroidSensusServiceConnection(AndroidSensusServiceBinder binder)
        {
            if (binder != null)
                _binder = binder;
        }

        public void OnServiceConnected(ComponentName name, IBinder binder)
        {
            _binder = binder as AndroidSensusServiceBinder;

            if (_binder != null)
                ServiceConnected(this, new AndroidServiceConnectedEventArgs(_binder));
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            _binder.SensusServiceHelper = null;
        }
    }
}