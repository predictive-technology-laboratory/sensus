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
using Xamarin.Geolocation;

namespace Sensus.Android
{
    public class AndroidApp : App
    {
        public static void Initialize(Geolocator locator, Context context)
        {
            Set(new AndroidApp(locator, context));
        }

        private Context _context;

        public Context Context
        {
            get { return _context; }
        }

        protected AndroidApp(Geolocator locator, Context context)
            : base(locator)
        {
            _context = context;
        }
    }
}