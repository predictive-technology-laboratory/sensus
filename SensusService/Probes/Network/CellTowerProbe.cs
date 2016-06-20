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
using Newtonsoft.Json;

namespace SensusService.Probes.Network
{
    /// <summary>
    /// Probes information about the cell tower to which the device is bound.
    /// </summary>
    public abstract class CellTowerProbe : ListeningProbe
    {
        /// <summary>
        /// TODO:  Need to verify the effect of this setting. Are updates received while device is asleep? Also update messages below.
        /// </summary>
        /// <value>False.</value>
        [JsonIgnore]
        protected override bool DefaultKeepDeviceAwake
        {
            get
            {
                return false;
            }
        }

        [JsonIgnore]
        protected override string DeviceAwakeWarning
        {
            get
            {
                return "This setting does not affect iOS. On Android, all cell tower updates will be received, and this will consume more power.";
            }
        }

        [JsonIgnore]
        protected override string DeviceAsleepWarning
        {
            get
            {
                return "This setting does not affect iOS. On Android, cell tower updates will be paused while the device is sleeping, and this will conserve power.";
            }
        }

        public sealed override string DisplayName
        {
            get { return "Cell Tower Binding"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(CellTowerDatum); }
        }
    }
}
