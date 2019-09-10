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
using System.Linq;
using Sensus.Exceptions;
using Sensus.Probes;

namespace Sensus.Android
{
	public class AndroidPowerConnectionChangeListener : PowerConnectionChangeListener
	{
		public AndroidPowerConnectionChangeListener()
		{
			AndroidPowerConnectionChangeBroadcastReceiver.POWER_CONNECTION_CHANGED += async (sender, connected) =>
			{
				try
				{
					bool listenOnACPower = SensusServiceHelper.Get().GetRunningProtocols().SelectMany(x => x.Probes.OfType<ListeningProbe>()).Any(x => x.KeepDeviceAwakeOnAcPower);

					if (connected && listenOnACPower)
					{
						await SensusServiceHelper.Get().KeepDeviceAwakeAsync();
					}
					else
					{
						await SensusServiceHelper.Get().LetDeviceSleepAsync();
					}

					PowerConnectionChanged?.Invoke(sender, connected);
				}
				catch (Exception ex)
				{
					SensusException.Report("Failed to process power connection change.", ex);
				}
			};
		}
	}
}