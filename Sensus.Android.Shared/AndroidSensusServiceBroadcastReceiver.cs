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

using Android.App;
using Android.Content;
using Sensus.Exceptions;
using System;
using System.Threading.Tasks;

namespace Sensus.Android
{
    [BroadcastReceiver]
    public class AndroidSensusServiceBroadcastReceiver : BroadcastReceiver
    {

        public override void OnReceive(global::Android.Content.Context context, Intent intent)
        {
            // this method is usually called on the UI thread and can crash the app if it throws an exception
            try
            {
                if (intent == null)
                {
                    throw new ArgumentNullException(nameof(intent));
                }

                AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;
                if (serviceHelper != null)
                {

                    if (intent.Action == AndroidSensusService.SERVICE_PROTOCOL_START_ACTION)
                    {
                        foreach (var protocol in serviceHelper.RegisteredProtocols)
                        {
                            if (serviceHelper.RunningProtocolIds.Contains(protocol.Id) == false)
                            {
                                Task.Run(async () => await serviceHelper.RunActionUsingMainActivityAsync(mainActivity =>
                                {
                                    mainActivity.RunOnUiThread(async () =>
                                    {
                                        await protocol.StartAsync(System.Threading.CancellationToken.None);

                                    });

                                }, true, false));
                                //Task.Run(async () => await protocol.StartAsync(System.Threading.CancellationToken.None)).Wait();
                            }
                        }
                    }
                    else if (intent.Action == AndroidSensusService.SERVICE_PROTOCOL_STOP_ACTION)
                    {
                        foreach (var protocol in serviceHelper.RegisteredProtocols)
                        {
                            if (serviceHelper.RunningProtocolIds.Contains(protocol.Id) == true)
                            {
                                Task.Run(async () => await serviceHelper.RunActionUsingMainActivityAsync(mainActivity =>
                                {
                                    mainActivity.RunOnUiThread(async () =>
                                    {
                                        await serviceHelper.StopProtocolsAsync();

                                    });

                                }, true, false));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception in power connection change broadcast receiver:  " + ex.Message, ex);
            }
        }
    }
}