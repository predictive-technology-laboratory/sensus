//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Newtonsoft.Json;
using System;
using Sensus.Exceptions;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sensus.DataStores
{
    /// <summary>
    /// <see cref="DataStore"/>s coordinate the compression, encryption, and transmission of data produced by <see cref="Probes.Probe"/>s, both
    /// on the user's device as well as remotely.
    /// </summary>
    public abstract class DataStore
    {
        public event EventHandler<string> UpdatedCaptionText;

        private bool _running;
        private Protocol _protocol;
        private string _captionText;

        [JsonIgnore]
        public bool Running
        {
            get { return _running; }
        }

        public Protocol Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        public string CaptionText
        {
            get { return _captionText; }
            protected set
            {
                _captionText = value;
                UpdatedCaptionText?.Invoke(this, _captionText);
            }
        }

        [JsonIgnore]
        public abstract string DisplayName { get; }

        protected DataStore()
        {            
            _running = false;
        }

        public virtual Task StartAsync()
        {
            if (!_running)
            {
                _running = true;
                SensusServiceHelper.Get().Logger.Log("Starting.", LoggingLevel.Normal, GetType());
            }

            return Task.CompletedTask;
        }

        public virtual Task StopAsync()
        {
            if (_running)
            {
                _running = false;
                SensusServiceHelper.Get().Logger.Log("Stopping.", LoggingLevel.Normal, GetType());
            }

            return Task.CompletedTask;
        }

        public async Task RestartAsync()
        {
            await StopAsync();
            await StartAsync();
        }

        public virtual Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = HealthTestResult.Okay;

            if (!_running)
            {
                result = HealthTestResult.Restart;
            }

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Running", _running.ToString() }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new AnalyticsTrackedEvent(eventName, properties));

            return Task.FromResult(result);
        }

        public virtual void Reset()
        {
            if (_running)
            {
                throw new Exception("Cannot reset data store while it is running.");
            }
        }

        public DataStore Copy()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };

            try
            {
                SensusServiceHelper.Get().FlashNotificationsEnabled = false;
                return JsonConvert.DeserializeObject<DataStore>(JsonConvert.SerializeObject(this, settings), settings);
            }
            catch (Exception ex)
            {
                string message = $"Failed to copy data store:  {ex.Message}";
                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                SensusException.Report(message, ex);
                return null;
            }
            finally
            {
                SensusServiceHelper.Get().FlashNotificationsEnabled = true;
            }
        }
    }
}
