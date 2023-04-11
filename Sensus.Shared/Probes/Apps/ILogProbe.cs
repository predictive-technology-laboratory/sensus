namespace Sensus.Probes.Apps
{
	public interface ILogProbe : IProbe
	{
		bool Enabled { get; }
		void AttachToLogger();
	}
}
