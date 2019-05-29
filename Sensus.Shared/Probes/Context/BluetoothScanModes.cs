using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Probes.Context
{
	[Flags]
	public enum BluetoothScanModes
	{
		Classic = 1,
		LE = 2,
		All = Classic | LE
	}
}
