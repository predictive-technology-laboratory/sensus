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
    public class SensusServiceBinder : Binder
    {
        private AndroidSensusService _service;

        public AndroidSensusService Service
        {
            get { return _service; }
            set { _service = value; }
        }

        public bool IsBound
        {
            get { return _service != null; }
        }

        public SensusServiceBinder(AndroidSensusService service)
        {
            _service = service;
        }
    }
}