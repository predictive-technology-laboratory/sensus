using System;

namespace Sensus
{
	[Flags]
	public enum MediaCacheModes
	{
		OnView = 0,
		OnSerialization = 1,
		OnDeserialization = 2,
		OnBoth = 3,
	}
}
