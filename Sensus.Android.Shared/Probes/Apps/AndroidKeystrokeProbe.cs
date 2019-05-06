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
using Sensus.Probes.Apps;

namespace Sensus.Android.Probes.Apps
{
    public class AndroidKeystrokeProbe : KeystrokeProbe
    {
        public AndroidKeystrokeProbe()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void StartListening()
        {
            base.StartListening();
        }

        protected override void StopListening()
        {
            base.StopListening();
        }
    }
}