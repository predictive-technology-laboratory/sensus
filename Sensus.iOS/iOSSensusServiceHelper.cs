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

using UIKit;
using Foundation;
using System;
using System.IO;
using System.Threading;
using Sensus.Probes;
using Sensus.Context;
using Sensus.Probes.Movement;
using Sensus.Probes.Location;
using Xamarin.Forms;
using MessageUI;
using AVFoundation;
using CoreBluetooth;
using System.Threading.Tasks;
using TTGSnackBar;
using WindowsAzure.Messaging;
using Newtonsoft.Json;
using Sensus.Exceptions;
using Plugin.Geolocator.Abstractions;

namespace Sensus.iOS
{
    public class iOSSensusServiceHelper : SensusServiceHelper
    {
        #region static members
        private const int BLUETOOTH_ENABLE_TIMEOUT_MS = 15000;
        #endregion

        private readonly string _deviceId;
        private readonly string _deviceModel;
        private readonly string _operatingSystem;
        private NSData _pushNotificationTokenData;
        private bool _keepAwakeEnabled;

        private object _keepAwakeLocker = new object();

        public override bool IsCharging
        {
            get
            {
                return UIDevice.CurrentDevice.BatteryState == UIDeviceBatteryState.Charging || UIDevice.CurrentDevice.BatteryState == UIDeviceBatteryState.Full;
            }
        }

        public override float BatteryChargePercent
        {
            get
            {
                return UIDevice.CurrentDevice.BatteryLevel * 100f;
            }
        }

        public override bool WiFiConnected
        {
            get
            {
                return NetworkConnectivity.LocalWifiConnectionStatus() == NetworkStatus.ReachableViaWiFiNetwork;
            }
        }

        public override string DeviceId
        {
            get
            {
                return _deviceId;
            }
        }

        public override string DeviceManufacturer
        {
            get { return "Apple"; }
        }

        public override string DeviceModel
        {
            get { return _deviceModel; }
        }

        public override string OperatingSystem
        {
            get
            {
                return _operatingSystem;
            }
        }

        public override string Version
        {
            get
            {
                return NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
            }
        }

        public override string PushNotificationToken
        {
            get
            {
                return _pushNotificationTokenData == null ? null : BitConverter.ToString(_pushNotificationTokenData.ToArray()).Replace("-", "").ToUpperInvariant();
            }
        }

        [JsonIgnore]
        public NSData PushNotificationTokenData
        {
            get
            {
                return _pushNotificationTokenData;
            }
            set
            {
                _pushNotificationTokenData = value;
            }
        }

        public iOSSensusServiceHelper()
        {
            // we've seen one case of a null reference when getting the identifier.
            try
            {
                _deviceId = UIDevice.CurrentDevice.IdentifierForVendor.AsString();

                if (string.IsNullOrWhiteSpace(_deviceId))
                {
                    throw new NullReferenceException("Null device ID.");
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception while obtaining device ID:  " + ex.Message, ex);

                // set an arbitrary identifier, since we couldn't get one above. this will change each 
                // time the app is killed and restarted. but the NRE condition above should be very rare.
                _deviceId = Guid.NewGuid().ToString();
            }

            try
            {
                _deviceModel = Xamarin.iOS.DeviceHardware.Version;
            }
            catch(Exception)
            {

            }

            try
            {
                _operatingSystem = UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
            }
            catch(Exception)
            {

            }
        }

        public override async Task KeepDeviceAwakeAsync()
        {
            lock (_keepAwakeLocker)
            {
                if (_keepAwakeEnabled)
                { 
                    Logger.Log("Attempted to keep device awake, but keep-awake is already enabled.", LoggingLevel.Normal, GetType());
                    return;
                }
                else
                {
                    _keepAwakeEnabled = true;
                }
            }

            Logger.Log("Enabling keep-awake by adding GPS listener.", LoggingLevel.Normal, GetType());
            await GpsReceiver.Get().AddListenerAsync(KeepAwakePositionChanged, false);
        }

        public override async Task LetDeviceSleepAsync()
        {
            lock (_keepAwakeLocker)
            {
                if (_keepAwakeEnabled)
                {
                    _keepAwakeEnabled = false;
                }
                else
                {
                    Logger.Log("Attempted to let device sleep, but keep-awake was already disabled.", LoggingLevel.Normal, GetType());
                    return;
                }
            }

            Logger.Log("Disabling keep-awake by removing GPS listener.", LoggingLevel.Normal, GetType());
            await GpsReceiver.Get().RemoveListenerAsync(KeepAwakePositionChanged);
        }

        private void KeepAwakePositionChanged(object sender, PositionEventArgs position)
        {
            Logger.Log("Received keep-awake position change.", LoggingLevel.Normal, GetType());
        }

        protected override Task ProtectedFlashNotificationAsync(string message)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                TTGSnackbar snackbar = new TTGSnackbar(message);
                snackbar.Duration = TimeSpan.FromSeconds(5);
                snackbar.Show();
            });

            return Task.CompletedTask;
        }

        public override Task ShareFileAsync(string path, string subject, string mimeType)
        {
            return SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                if (!MFMailComposeViewController.CanSendMail)
                {
                    await FlashNotificationAsync("You do not have any mail accounts configured. Please configure one before attempting to send emails from Sensus.");
                    return;
                }

                NSData data = NSData.FromUrl(NSUrl.FromFilename(path));

                if (data == null)
                {
                    await FlashNotificationAsync("No file to share.");
                    return;
                }

                MFMailComposeViewController mailer = new MFMailComposeViewController();
                mailer.SetSubject(subject);
                mailer.AddAttachmentData(data, mimeType, Path.GetFileName(path));
                mailer.Finished += (sender, e) => mailer.DismissViewControllerAsync(true);
                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(mailer, true, null);
            });
        }

        public override Task SendEmailAsync(string toAddress, string subject, string message)
        {
            return SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                if (MFMailComposeViewController.CanSendMail)
                {
                    MFMailComposeViewController mailer = new MFMailComposeViewController();
                    mailer.SetToRecipients(new string[] { toAddress });
                    mailer.SetSubject(subject);
                    mailer.SetMessageBody(message, false);
                    mailer.Finished += (sender, e) => mailer.DismissViewControllerAsync(true);
                    UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(mailer, true, null);
                }
                else
                {
                    await FlashNotificationAsync("You do not have any mail accounts configured. Please configure one before attempting to send emails from Sensus.");
                }
            });
        }

        public override Task TextToSpeechAsync(string text)
        {
            return Task.Run(() =>
            {
                try
                {
                    new AVSpeechSynthesizer().SpeakUtterance(new AVSpeechUtterance(text));
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed to speak utterance:  " + ex.Message, LoggingLevel.Normal, GetType());
                }
            });
        }

        public override Task<string> RunVoicePromptAsync(string prompt, Action postDisplayCallback)
        {
            return Task.Run(() =>
            {
                string input = null;
                ManualResetEvent dialogDismissWait = new ManualResetEvent(false);

                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    #region set up dialog
                    ManualResetEvent dialogShowWait = new ManualResetEvent(false);

                    UIAlertView dialog = new UIAlertView("Sensus is requesting input...", prompt, default(IUIAlertViewDelegate), "Cancel", "OK");
                    dialog.AlertViewStyle = UIAlertViewStyle.PlainTextInput;

                    dialog.Dismissed += (o, e) =>
                    {
                        dialogDismissWait.Set();
                    };

                    dialog.Presented += (o, e) =>
                    {
                        dialogShowWait.Set();

                        postDisplayCallback?.Invoke();
                    };

                    dialog.Clicked += (o, e) =>
                    {
                        if (e.ButtonIndex == 1)
                        {
                            input = dialog.GetTextField(0).Text;
                        }
                    };

                    dialog.Show();
                    #endregion

                    #region voice recognizer
                    Task.Run(() =>
                    {
                        // wait for the dialog to be shown so it doesn't hide our speech recognizer activity
                        dialogShowWait.WaitOne();

                        // there's a slight race condition between the dialog showing and speech recognition showing. pause here to prevent the dialog from hiding the speech recognizer.
                        Thread.Sleep(1000);

                        // TODO:  Add speech recognition
                    });
                    #endregion
                });

                dialogDismissWait.WaitOne();

                return input;
            });
        }

        public override bool EnableProbeWhenEnablingAll(Probe probe)
        {
            // polling for locations doesn't work very well in iOS, since it depends on the user. don't enable probes that need location polling by default.
            return !(probe is PollingLocationProbe) &&
            !(probe is PollingSpeedProbe) &&
            !(probe is PollingPointsOfInterestProximityProbe);
        }

        public override ImageSource GetQrCodeImageSource(string contents)
        {
            return ImageSource.FromStream(() =>
            {
                UIImage bitmap = BarcodeWriter.Write(contents);
                MemoryStream ms = new MemoryStream();
                bitmap.AsPNG().AsStream().CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            });
        }

        protected override async Task RegisterWithNotificationHubAsync(Tuple<string, string> hubSas)
        {
            SBNotificationHub notificationHub = new SBNotificationHub(hubSas.Item2, hubSas.Item1);
            await notificationHub.RegisterNativeAsyncAsync(_pushNotificationTokenData, new NSSet());
        }

        protected override async Task UnregisterFromNotificationHubAsync(Tuple<string, string> hubSas)
        {
            SBNotificationHub notificationHub = new SBNotificationHub(hubSas.Item2, hubSas.Item1);
            await notificationHub.UnregisterAllAsyncAsync(_pushNotificationTokenData);
        }

        protected override void RequestNewPushNotificationToken()
        {
            // reregister for remote notifications to get a new token
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UIApplication.SharedApplication.UnregisterForRemoteNotifications();
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            });
        }

        /// <summary>
        /// Enables the Bluetooth adapter, or prompts the user to do so if we cannot do this programmatically.
        /// </summary>
        /// <returns><c>true</c>, if Bluetooth was enabled, <c>false</c> otherwise.</returns>
        /// <param name="lowEnergy">If set to <c>true</c> low energy.</param>
        /// <param name="rationale">Rationale.</param>
        public override async Task<bool> EnableBluetoothAsync(bool lowEnergy, string rationale)
        {
            return await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                try
                {
                    TaskCompletionSource<bool> enableTaskCompletionSource = new TaskCompletionSource<bool>();

                    CBCentralManager manager = new CBCentralManager();

                    manager.UpdatedState += (sender, e) =>
                    {
                        if (manager.State == CBCentralManagerState.PoweredOn)
                        {
                            enableTaskCompletionSource.TrySetResult(true);
                        }
                    };

                    if (manager.State == CBCentralManagerState.PoweredOn)
                    {
                        enableTaskCompletionSource.TrySetResult(true);
                    }

                    Task timeoutTask = Task.Delay(BLUETOOTH_ENABLE_TIMEOUT_MS);

                    if (await Task.WhenAny(enableTaskCompletionSource.Task, timeoutTask) == timeoutTask)
                    {
                        Logger.Log("Timed out while waiting for user to enable Bluetooth.", LoggingLevel.Normal, GetType());
                        enableTaskCompletionSource.TrySetResult(false);
                    }

                    return await enableTaskCompletionSource.Task;
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed while requesting Bluetooth enable:  " + ex.Message, LoggingLevel.Normal, GetType());
                    return false;
                }
            });
        }

        /// <summary>
        /// Not available on iOS. Will always return a completed <see cref="Task"/> with a result of false.
        /// </summary>
        /// <returns>False</returns>
        /// <param name="reenable">If set to <c>true</c> reenable.</param>
        /// <param name="lowEnergy">If set to <c>true</c> low energy.</param>
        /// <param name="rationale">Rationale.</param>
        public override Task<bool> DisableBluetoothAsync(bool reenable, bool lowEnergy, string rationale)
        {
            return Task.FromResult(false);
        }
    }
}