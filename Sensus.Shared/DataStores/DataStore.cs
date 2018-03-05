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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sensus.UI.UiProperties;
using Sensus.Exceptions;
using Sensus.DataStores.Remote;
using Sensus.Context;
using Sensus.Callbacks;

namespace Sensus.DataStores
{
    /// <summary>
    /// Data Stores play an integral part in the Sensus system. They are the storage location for data that come off of <see cref="Probes.Probe"/>s.
    /// </summary>
    public abstract class DataStore
    {
        private bool _running;
        private Protocol _protocol;

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

        public virtual bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            if (!_running)
            {
                error += "Data store \"" + GetType().FullName + "\" is not running." + Environment.NewLine;
                restart = true;
            }

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