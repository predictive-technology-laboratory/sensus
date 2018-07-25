﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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

using Sensus.Exceptions;
using System;
using UIKit;

namespace Sensus.iOS
{
    public class iOSPowerConnectionChangeListener : PowerConnectionChangeListener
    {
        public iOSPowerConnectionChangeListener()
        {
            UIDevice.Notifications.ObserveBatteryStateDidChange((sender, e) =>
            {
                try
                {
                    UIDeviceBatteryState batteryState = UIDevice.CurrentDevice.BatteryState;
                    bool connected = batteryState == UIDeviceBatteryState.Charging || batteryState == UIDeviceBatteryState.Full;
                    PowerConnectionChanged?.Invoke(this, connected);
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed to process power connection change.", ex);
                }
            });
        }
    }
}
