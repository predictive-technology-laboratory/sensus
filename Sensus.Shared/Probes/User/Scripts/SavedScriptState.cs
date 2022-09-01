using Newtonsoft.Json;
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
			Variables = new Dictionary<string, object>();
		}

		public SavedScriptState(string savePath) : this()
		{
			SavePath = savePath;
		}

		[JsonIgnore]
		public string SavePath { get; set; }
		[JsonIgnore]
		public Stack<int> InputGroupStack { get; set; }
		public Dictionary<string, ScriptDatum> SavedInputs { get; set; }
		public Dictionary<string, object> Variables { get; set; }

		[JsonIgnore]
		public bool Restored { get; set; }

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
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(SavePath));

				using (StreamWriter writer = new StreamWriter(SavePath))
				{
					await writer.WriteAsync(JsonConvert.SerializeObject(this));
				}
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get()?.Logger.Log("Failed to save script state: " + e.Message, LoggingLevel.Normal, GetType());
			}
		}
	}
}
