using Sensus.UI.UiProperties;

namespace Sensus.UI.Inputs
{
	public class ScriptSchedulerInput : DateTimeInput
	{
		[ListUiProperty("Scheduled Script:", true, 14, new object[] { ScheduleModes.Self, ScheduleModes.Next }, true)]
		public ScheduleModes ScheduleMode { get; set; }

		[EntryIntegerUiProperty("Days in Future:", true, 15, false)]
		public int DaysInFuture { get; set; }

		public override bool RequireFutureDate => true;

		public override string DefaultName
		{
			get
			{
				return "Script Scheduler";
			}
		}
	}


}
