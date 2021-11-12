using Sensus.UI.Inputs;
using System.Collections.Generic;

namespace Sensus.Probes.User.Scripts
{
	public class SavedScriptState
	{
		public SavedScriptState()
		{
			InputGroupStack = new Stack<int>();
			SavedInputs = new Dictionary<string, ScriptDatum>();
			Variables = new Dictionary<string, string>();
		}

		public Stack<int> InputGroupStack { get; set; }
		public Dictionary<string, ScriptDatum> SavedInputs { get; set; }
		public Dictionary<string, string> Variables { get; set; }
	}
}
