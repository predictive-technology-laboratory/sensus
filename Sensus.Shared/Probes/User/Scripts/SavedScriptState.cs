using Newtonsoft.Json;
using Sensus.UI.Inputs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

		public SavedScriptState(string savePath) : this()
		{
			SavePath = savePath;
		}

		public string SavePath { get; set; }
		[JsonIgnore]
		public Stack<int> InputGroupStack { get; set; }
		public Dictionary<string, ScriptDatum> SavedInputs { get; set; }
		public Dictionary<string, string> Variables { get; set; }

		[JsonProperty("InputGroupStack")]
		public List<int> InputGroupList
		{
			get
			{
				return InputGroupStack.ToList();
			}
			set
			{
				if (value != null)
				{
					InputGroupStack.Clear();

					foreach (int inputGroup in value)
					{
						InputGroupStack.Push(inputGroup);
					}
				}
				else
				{
					InputGroupStack.Clear();
				}
			}
		}

		public async Task SaveAsync()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(SavePath));

			using (StreamWriter writer = new StreamWriter(SavePath))
			{
				await writer.WriteAsync(JsonConvert.SerializeObject(this));
			}
		}
	}
}
