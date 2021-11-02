using Sensus.UI.Inputs;
using System.Collections.Generic;

namespace Sensus.Probes.User.Scripts
{
	public class SavedScriptRunState
	{
		public SavedScriptRunState()
		{
			PresentedInputGroupPositions = new List<int>();
			Variables = new Dictionary<string, string>();
		}

		public List<int> PresentedInputGroupPositions { get; set; }
		public Dictionary<string, string> Variables { get; set; }
	}
}
