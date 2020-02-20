using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes
{
	// this was added as a potential refactor to how ListeningProbe.KeepDeviceAwake worked, 
	public enum KeepDeviceAwakeConditions
	{
		Never,
		OnAcPower,
		Always
	}
}
