using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.UI.Inputs
{
	public class ScriptSchedulerInput : DateTimeInput
	{
		[ListUiProperty("Schedule Mode:", true, 14, new object[] { ScheduleModes.Self, ScheduleModes.Next }, true)]
		public ScheduleModes ScheduleMode { get; set; }

		[EntryIntegerUiProperty("Days from Now:", true, 15, false)]
		public int DaysFromNow { get; set; }

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
