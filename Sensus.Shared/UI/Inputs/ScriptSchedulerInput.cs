using Sensus.Probes.User.Scripts;
using Sensus.UI.UiProperties;

namespace Sensus.UI.Inputs
{
	public class ScriptSchedulerInput : DateTimeInput
	{
		public ScriptRunner Runner { get; set; }

		[ListUiProperty("Schedule Mode:", true, 14, new object[] { ScheduleModes.Reminder, ScheduleModes.Self, ScheduleModes.Next, ScheduleModes.Select }, true)]
		public ScheduleModes ScheduleMode { get; set; }

		[ScriptsUiProperty("Scheduled Script:", true, 15, false)]
		public ScriptRunner ScheduledScript { get; set; }

		[EntryIntegerUiProperty("Days in Future:", true, 15, false)]
		public int DaysInFuture { get; set; }

		[EntryStringUiProperty("Notification Message", true, 16, false)]
		public string NotificationMessage { get; set; }

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
