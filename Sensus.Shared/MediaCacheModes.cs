using System;

namespace Sensus
{
	[Flags]
	public enum MediaCacheModes
	{
		OnSerialization = 1,
		OnDeserialization = 2,
		OnBoth = 3,
	}
}
