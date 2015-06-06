using SensusService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sensus.WinPhone
{
    public class WinPhoneSensusServiceHelper : SensusServiceHelper
    {
        public override bool IsCharging
        {
            get { throw new NotImplementedException(); }
        }

        public override bool WiFiConnected
        {
            get { throw new NotImplementedException(); }
        }

        public override string DeviceId
        {
            get { throw new NotImplementedException(); }
        }

        public override string OperatingSystem
        {
            get { throw new NotImplementedException(); }
        }

        protected override Xamarin.Geolocation.Geolocator Geolocator
        {
            get { throw new NotImplementedException(); }
        }

        protected override void InitializeXamarinInsights()
        {
            throw new NotImplementedException();
        }

        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, string userNotificationMessage)
        {
            throw new NotImplementedException();
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS, string userNotificationMessage)
        {
            throw new NotImplementedException();
        }

        protected override void UnscheduleCallback(string callbackId, bool repeating)
        {
            throw new NotImplementedException();
        }

        public override void PromptForAndReadTextFileAsync(string promptTitle, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public override void ShareFileAsync(string path, string subject)
        {
            throw new NotImplementedException();
        }

        public override void TextToSpeechAsync(string text, Action callback)
        {
            throw new NotImplementedException();
        }

        public override void PromptForInputAsync(string prompt, bool startVoiceRecognizer, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public override void IssueNotificationAsync(string message, string id)
        {
            throw new NotImplementedException();
        }

        public override void FlashNotificationAsync(string message, Action callback)
        {
            throw new NotImplementedException();
        }

        public override void KeepDeviceAwake()
        {
            throw new NotImplementedException();
        }

        public override void LetDeviceSleep()
        {
            throw new NotImplementedException();
        }

        public override void UpdateApplicationStatus(string status)
        {
            throw new NotImplementedException();
        }

        public override bool EnableProbeWhenEnablingAll(SensusService.Probes.Probe probe)
        {
            throw new NotImplementedException();
        }
    }
}
