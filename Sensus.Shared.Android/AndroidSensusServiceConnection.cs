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

using Android.Content;
using Android.OS;
using System;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Shared.Android
{
    public class AndroidSensusServiceConnection<MainActivityT> : Java.Lang.Object, IServiceConnection where MainActivityT : FormsApplicationActivity
    {
        public event EventHandler<AndroidServiceConnectedEventArgs<MainActivityT>> ServiceConnected;
        public event EventHandler<AndroidServiceConnectedEventArgs<MainActivityT>> ServiceDisconnected;

        private AndroidSensusServiceBinder<MainActivityT> _binder;

        public AndroidSensusServiceBinder<MainActivityT> Binder
        {
            get { return _binder; }
        }

        public AndroidSensusServiceConnection()
        {
            _binder = null;
        }

        public void OnServiceConnected(ComponentName name, IBinder binder)
        {
            _binder = binder as AndroidSensusServiceBinder<MainActivityT>;

            if (_binder != null && ServiceConnected != null)
                ServiceConnected(this, new AndroidServiceConnectedEventArgs<MainActivityT>(_binder));
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            if (_binder != null)
            {
                if (ServiceDisconnected != null)
                    ServiceDisconnected(this, new AndroidServiceConnectedEventArgs<MainActivityT>(_binder));

                _binder.SensusServiceHelper = null;
            }
        }
    }
}
