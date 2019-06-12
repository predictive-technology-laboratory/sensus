﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using Sensus.Exceptions;
using Sensus.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Sensus.Android.Notifications
{
    [BroadcastReceiver]
    public class ForegroundServiceNotificationActionReceiver : BroadcastReceiver
    {
        public override async void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

                // service helper will be null for a time when the app is starting up
                if (serviceHelper != null)
                {
                    if (intent.Action == AndroidNotifier.FOREGROUND_SERVICE_NOTIFICATION_ACTION_PAUSE)
                    {
                        IEnumerable<Protocol> pausableProtocols = serviceHelper.RegisteredProtocols.Where(protocol => protocol.AllowPause);

						await (Application.Current as App).DetailPage.Navigation.PushAsync(new SnoozeProtocolsPage(pausableProtocols));
                    }
                    else if (intent.Action == AndroidNotifier.FOREGROUND_SERVICE_NOTIFICATION_ACTION_RESUME)
                    {
                        foreach (Protocol protocol in serviceHelper.RegisteredProtocols)
                        {
                            await protocol.ResumeAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while handling notification action:  " + ex.Message, ex);
            }
        }
    }
}