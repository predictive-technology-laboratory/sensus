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
using CoreFoundation;
using System.Threading.Tasks;
using TTGSnackBar;
using WindowsAzure.Messaging;
using Newtonsoft.Json;
using Sensus.Exceptions;
using UserNotifications;

namespace Sensus.iOS
{
    public class iOSSensusServiceHelper : SensusServiceHelper
    {
        #region static members
        private const int BLUETOOTH_ENABLE_TIMEOUT_MS = 15000;
        #endregion

        private DateTime _nextToastTime;
        private readonly object _toastLocker = new object();
        private NSData _pushNotificationTokenData;

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
                return UIDevice.CurrentDevice.IdentifierForVendor.AsString();
            }
        }

        public override string DeviceManufacturer
        {
            get { return "Apple"; }
        }

        public override string DeviceModel
        {
            get { return Xamarin.iOS.DeviceHardware.Version; }
        }

        public override string OperatingSystem
        {
            get
            {
                return UIDevice.CurrentDevice.SystemName + " " + UIDevice.CurrentDevice.SystemVersion;
            }
        }

        protected override bool IsOnMainThread
        {
            get { return NSThread.IsMain; }
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
            _nextToastTime = DateTime.Now;
        }

        protected override Task ProtectedFlashNotificationAsync(string message)
        {
            return Task.Run(() =>
            {
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    TTGSnackbar snackbar = new TTGSnackbar(message);
                    snackbar.Duration = TimeSpan.FromSeconds(5);
                    snackbar.Show();
                });
            });
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

                        if (postDisplayCallback != null)
                        {
                            postDisplayCallback();
                        }
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

        protected override void RegisterWithNotificationHub(Tuple<string, string> hubSas)
        {
            SBNotificationHub notificationHub = new SBNotificationHub(hubSas.Item2, hubSas.Item1);

            bool registered = notificationHub.RegisterNative(_pushNotificationTokenData, new NSSet(), out NSError error);

            if (error != null)
            {
                SensusException.Report("Exception while registering from notification hub.", new Exception(error.ToString()));
            }
        }

        protected override void UnregisterFromNotificationHub(Tuple<string, string> hubSas)
        {
            SBNotificationHub notificationHub = new SBNotificationHub(hubSas.Item2, hubSas.Item1);

            bool unregistered = notificationHub.UnregisterAll(_pushNotificationTokenData, out NSError error);

            if (error != null)
            {
                SensusException.Report("Exception while unregistering from notification hub.", new Exception(error.ToString()));
            }
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
        /// Enables the Bluetooth adapter, or prompts the user to do so if we cannot do this programmatically. Must not be called from the UI thread.
        /// </summary>
        /// <returns><c>true</c>, if Bluetooth was enabled, <c>false</c> otherwise.</returns>
        /// <param name="lowEnergy">If set to <c>true</c> low energy.</param>
        /// <param name="rationale">Rationale.</param>
        public override bool EnableBluetooth(bool lowEnergy, string rationale)
        {
            base.EnableBluetooth(lowEnergy, rationale);

            bool enabled = false;
            ManualResetEvent enableWait = new ManualResetEvent(false);

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    CBCentralManager manager = new CBCentralManager(DispatchQueue.MainQueue);

                    manager.UpdatedState += (sender, e) =>
                    {
                        if (manager.State == CBCentralManagerState.PoweredOn)
                        {
                            enabled = true;
                            enableWait.Set();
                        }
                    };

                    if (manager.State == CBCentralManagerState.PoweredOn)
                    {
                        enabled = true;
                        enableWait.Set();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed while requesting Bluetooth enable:  " + ex.Message, LoggingLevel.Normal, GetType());
                    enableWait.Set();
                }
            });

            // the base class will ensure that we're not on the main thread, making the following wait okay.
            if (!enableWait.WaitOne(BLUETOOTH_ENABLE_TIMEOUT_MS))
            {
                Logger.Log("Timed out while waiting for user to enable Bluetooth.", LoggingLevel.Normal, GetType());
            }

            return enabled;
        }

        #region methods not implemented in ios

        public override Task PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            return Task.Run(async () =>
            {
                await FlashNotificationAsync("This is not supported on iOS.");
            });
        }

        public override void KeepDeviceAwake()
        {
        }

        public override void LetDeviceSleep()
        {
        }

        public override Task BringToForegroundAsync()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}