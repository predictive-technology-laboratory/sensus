#region copyright
// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
 
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