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

using System;
using Sensus.Probes;
using Syncfusion.SfChart.XForms;
using Android.App;
using Android.Gms.Common.Apis;
using Android.Gms.Awareness;
using Android.Content;
using Android.Gms.Awareness.Fence;
using Sensus.Exceptions;
using Sensus.Probes.Movement;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Android.Gms.Common;
using Plugin.Permissions.Abstractions;
using System.Threading.Tasks;
using Plugin.Permissions;

namespace Sensus.Android.Probes.Movement
{
    public class AndroidActivityProbe : ListeningProbe
    {
        public const string ACTIVITY_RECOGNITION_ACTION = "SENSUS_ACTIVITY_RECOGNITION";

        private GoogleApiClient _awarenessApiClient;
        private Dictionary<string, AndroidActivityProbeBroadcastReceiver> _activityReciever;

        [JsonIgnore]
        public override Type DatumType
        {
            get
            {
                return typeof(ActivityDatum);
            }
        }

        [JsonIgnore]
        public override string DisplayName
        {
            get
            {
                return "Activity";
            }
        }

        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return null;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting should not be enabled. It does not affect iOS and will unnecessarily reduce battery life on Android.";
            }
        }

        public AndroidActivityProbe()
        {
            _activityReciever = new Dictionary<string, AndroidActivityProbeBroadcastReceiver>();

            CreateReceiver(nameof(DetectedActivityFence.InVehicle) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.InVehicle) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.InVehicle) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.OnBicycle) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.OnBicycle) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.OnBicycle) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.OnFoot) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.OnFoot) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.OnFoot) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.Running) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.Running) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.Running) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.Still) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.Still) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.Still) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.Tilting) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.Tilting) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.Tilting) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.Walking) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.Walking) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.Walking) + ".Stopping");

            CreateReceiver(nameof(DetectedActivityFence.Unknown) + ".Starting");
            CreateReceiver(nameof(DetectedActivityFence.Unknown) + ".During");
            CreateReceiver(nameof(DetectedActivityFence.Unknown) + ".Stopping");
        }

        private void CreateReceiver(string activityName)
        {
            AndroidActivityProbeBroadcastReceiver receiver = new AndroidActivityProbeBroadcastReceiver();

            receiver.ActivityChanged += async (sender, activityDatum) =>
            {
                await StoreDatumAsync(activityDatum);
            };

            _activityReciever.Add(activityName, receiver);
        }

        protected override void Initialize()
        {
            base.Initialize();

            int googlePlayServicesAvailability = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Application.Context);

            if (googlePlayServicesAvailability != ConnectionResult.Success)
            {
                string message = "Google Play Services are not available on this device.";

                if (googlePlayServicesAvailability == ConnectionResult.ServiceVersionUpdateRequired)
                {
                    message += " Please update your phone's Google Play Services app using the App Store. Then restart your study.";
                }

                message += " Email the study organizers and tell them you received the following error code:  " + googlePlayServicesAvailability;

                // the problem we've encountered is potentially fixable, so do not throw a NotSupportedException, as doing this would
                // disable the probe and prevent any future restart attempts from succeeding.
                throw new Exception(message);
            }

            // connected to the awareness client
            _awarenessApiClient = new GoogleApiClient.Builder(Application.Context).AddApi(Awareness.Api)

                .AddConnectionCallbacks(

                    bundle =>
                    {
                        SensusServiceHelper.Get().Logger.Log("Connected to Google Awareness API.", LoggingLevel.Normal, GetType());
                    },

                    status =>
                    {
                        SensusServiceHelper.Get().Logger.Log("Connection to Google Awareness API suspended. Status:  " + status, LoggingLevel.Normal, GetType());
                    })

                .Build();

            _awarenessApiClient.BlockingConnect();

            if (_awarenessApiClient.IsConnected)
            {
                if (SensusServiceHelper.Get().ObtainPermission(Permission.Location) != PermissionStatus.Granted)
                {
                    string error = "Geolocation is not permitted on this device. Cannot start location fences.";
                    SensusServiceHelper.Get().FlashNotificationAsync(error);
                }
            }
            else
            {
                throw new Exception("Failed to connect with Google Awareness API.");
            }
        }

        protected override void StartListening()
        {
            AddFence(DetectedActivityFence.InVehicle, nameof(DetectedActivityFence.InVehicle));
            AddFence(DetectedActivityFence.OnBicycle, nameof(DetectedActivityFence.OnBicycle));
            AddFence(DetectedActivityFence.OnFoot, nameof(DetectedActivityFence.OnFoot));
            AddFence(DetectedActivityFence.Running, nameof(DetectedActivityFence.Running));
            AddFence(DetectedActivityFence.Still, nameof(DetectedActivityFence.Still));
            AddFence(DetectedActivityFence.Tilting, nameof(DetectedActivityFence.Tilting));
            AddFence(DetectedActivityFence.Unknown, nameof(DetectedActivityFence.Unknown));
            AddFence(DetectedActivityFence.Walking, nameof(DetectedActivityFence.Walking));

            // start monitoring location changes
            Task.Run(async () =>
            {
                if (await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location) == PermissionStatus.Granted)
                {
                    // get current location and start fence when obtained
                }
            });
        }

        private void AddFence(int activityId, string activityName)
        {
            string baseId = ACTIVITY_RECOGNITION_ACTION + "." + activityName;

            FenceUpdateRequestBuilder requestBuilder = new FenceUpdateRequestBuilder();

            string fenceId;

            fenceId = baseId + ".Starting";
            requestBuilder.AddFence(fenceId, DetectedActivityFence.Starting(activityId), GetFencePendingIntent(fenceId));
            Application.Context.RegisterReceiver(_activityReciever[activityName], new IntentFilter(fenceId));

            fenceId = baseId + ".During";
            requestBuilder.AddFence(fenceId, DetectedActivityFence.During(activityId), GetFencePendingIntent(fenceId));
            Application.Context.RegisterReceiver(_activityReciever[activityName], new IntentFilter(fenceId));

            fenceId = baseId + ".Stopping";
            requestBuilder.AddFence(fenceId, DetectedActivityFence.Stopping(activityId), GetFencePendingIntent(fenceId));
            Application.Context.RegisterReceiver(_activityReciever[activityName], new IntentFilter(fenceId));

            UpdateFences(requestBuilder.Build());
        }

        private PendingIntent GetFencePendingIntent(string fenceId)
        {
            Intent activityRecognitionCallbackIntent = new Intent(fenceId);
            return PendingIntent.GetBroadcast(Application.Context, 0, activityRecognitionCallbackIntent, 0);
        }

        private bool UpdateFences(IFenceUpdateRequest updateRequest)
        {
            ManualResetEvent updateWait = new ManualResetEvent(false);

            bool success = false;

            try
            {
                // update fences is asynchronous
                Awareness.FenceApi.UpdateFences(_awarenessApiClient, updateRequest).SetResultCallback<Statuses>(status =>
                {
                    try
                    {
                        if (status.IsSuccess)
                        {
                            SensusServiceHelper.Get().Logger.Log("Updated Google Awareness API fences.", LoggingLevel.Normal, GetType());
                            success = true;
                        }
                        else if (status.IsCanceled)
                        {
                            SensusServiceHelper.Get().Logger.Log("Google Awareness API fence update canceled.", LoggingLevel.Normal, GetType());
                        }
                        else if (status.IsInterrupted)
                        {
                            SensusServiceHelper.Get().Logger.Log("Google Awareness API fence update interrupted", LoggingLevel.Normal, GetType());
                        }
                        else
                        {
                            string message = "Unrecognized fence update status:  " + status;
                            SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                            SensusException.Report(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while processing update status:  " + ex, LoggingLevel.Normal, GetType());
                    }
                    finally
                    {
                        // ensure that wait is always set
                        updateWait.Set();
                    }
                });
            }
            // catch any errors from calling UpdateFences
            catch (Exception ex)
            {
                // ensure that wait is always set
                SensusServiceHelper.Get().Logger.Log("Exception while updating fences:  " + ex, LoggingLevel.Normal, GetType());
                updateWait.Set();
            }

            // we've seen cases where the update blocks indefinitely (e.g., due to outdated google play services on the phone). impose
            // a timeout to avoid such blocks.
            if (!updateWait.WaitOne(TimeSpan.FromSeconds(60)))
            {
                SensusServiceHelper.Get().Logger.Log("Timed out while updating fences.", LoggingLevel.Normal, GetType());
            }

            return success;
        }

        protected override void StopListening()
        {
            // remove fences
            RemoveFence(nameof(DetectedActivityFence.InVehicle));
            RemoveFence(nameof(DetectedActivityFence.OnBicycle));
            RemoveFence(nameof(DetectedActivityFence.OnFoot));
            RemoveFence(nameof(DetectedActivityFence.Running));
            RemoveFence(nameof(DetectedActivityFence.Still));
            RemoveFence(nameof(DetectedActivityFence.Tilting));
            RemoveFence(nameof(DetectedActivityFence.Unknown));
            RemoveFence(nameof(DetectedActivityFence.Walking));

            // disconnect client
            _awarenessApiClient.Disconnect();
        }

        private void RemoveFence(string activityName)
        {
            try
            {
                IFenceUpdateRequest removeFenceRequest = new FenceUpdateRequestBuilder()
                    .RemoveFence(ACTIVITY_RECOGNITION_ACTION + "." + activityName)
                    .Build();

                if (!UpdateFences(removeFenceRequest))
                {
                    // we'll catch this immediately
                    throw new Exception("Failed to remove fence (e.g., timed out).");
                }
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while removing fence:  " + ex, LoggingLevel.Normal, GetType());
            }

            // unconditionally unregister the receiver. we may have failed to remove the fence for a variety of reasons, but 
            // the caller wishes to discontinue updates from the fence.
            try
            {
                Application.Context.UnregisterReceiver(_activityReciever[activityName]);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while unregistering receiver:  " + ex, LoggingLevel.Normal, GetType());
            }
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }
    }
}