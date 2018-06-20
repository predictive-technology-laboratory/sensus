using Sensus.Probes.Device;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Sensus.iOS.Probes.Context
{
    /// <summary>
    ///  the batteryState method will always just return UIDeviceBatteryStateUnknown unless you have set the batteryMonitoringEnabled property to YES. 
    /// </summary>
    public class iOSPowerConnectionChange : PowerConnectionChange
    {
        public iOSPowerConnectionChange(UIDevice uIDevice)
        {
            if(POWER_CONNECTION_CHANGE != null)
            {
                UIDevice.Notifications.ObserveBatteryStateDidChange((sender, e) =>
                {
                    var state = UIDevice.CurrentDevice.BatteryState;
                    var connected = state == UIDeviceBatteryState.Charging || state == UIDeviceBatteryState.Full;
                    POWER_CONNECTION_CHANGE?.Invoke(this, connected);
               });
            }
        }
    }
}
