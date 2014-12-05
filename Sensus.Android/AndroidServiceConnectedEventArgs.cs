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
    public class AndroidServiceConnectedEventArgs
    {
        private AndroidSensusServiceBinder _binder;

        public AndroidSensusServiceBinder Binder
        {
            get { return _binder; }
            set { _binder = value; }
        }

        public AndroidServiceConnectedEventArgs(AndroidSensusServiceBinder binder)
        {
            _binder = binder;
        }
    }
}