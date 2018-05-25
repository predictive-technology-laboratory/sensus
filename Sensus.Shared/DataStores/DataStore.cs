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

using Newtonsoft.Json;
using System;
using Sensus.Exceptions;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;

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

        public virtual void Start()
        {
            if (!_running)
            {
                _running = true;
                SensusServiceHelper.Get().Logger.Log("Starting.", LoggingLevel.Normal, GetType());
            }
        }

        public virtual void Stop()
        {
            if (_running)
            {
                _running = false;
                SensusServiceHelper.Get().Logger.Log("Stopping.", LoggingLevel.Normal, GetType());
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public virtual bool TestHealth(ref List<Tuple<string, Dictionary<string, string>>> events)
        {
            bool restart = false;

            if (!_running)
            {
                restart = true;
            }

            string eventName = TrackedEvent.Health + ":" + GetType().Name;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "Running", _running.ToString() }
            };

            Analytics.TrackEvent(eventName, properties);

            events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));

            return restart;
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