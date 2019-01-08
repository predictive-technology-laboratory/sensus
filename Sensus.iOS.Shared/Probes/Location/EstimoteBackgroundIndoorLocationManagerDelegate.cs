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
using System.Threading.Tasks;
using Estimote.iOS.Indoor;
using Foundation;

namespace Sensus.iOS.Probes.Location
{
    public class EstimoteBackgroundIndoorLocationManagerDelegate : EILBackgroundIndoorLocationManagerDelegate
    {
        public event Func<EILOrientedPoint, EILPositionAccuracy, EILLocation, Task> UpdatedPositionAsync;

        public override async void DidUpdatePosition(EILBackgroundIndoorLocationManager locationManager, EILOrientedPoint position, EILPositionAccuracy positionAccuracy, EILLocation location)
        {
            await (UpdatedPositionAsync?.Invoke(position, positionAccuracy, location) ?? Task.CompletedTask);
        }

        public override void DidFailToUpdatePosition(EILBackgroundIndoorLocationManager locationManager, NSError error)
        {
            SensusServiceHelper.Get().Logger.Log("Failed to update indoor location position:  " + error, LoggingLevel.Normal, GetType());
        }
    }
}