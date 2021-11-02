using Sensus.UI.Inputs;
using System.Collections.Generic;

namespace Sensus.Probes.User.Scripts
{
	public class SavedScriptRunState
	{
		public SavedScriptRunState()
		{
			Inputs = new List<Input>();
			Variables = new Dictionary<string, string>();
		}

		public int Position { get; set; }
		public List<Input> Inputs { get; set; }
		public Dictionary<string, string> Variables { get; set; }
	}
}
