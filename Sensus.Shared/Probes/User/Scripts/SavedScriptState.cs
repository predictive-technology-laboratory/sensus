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
			InputGroupStack = new();
			SavedInputs = new();
			Variables = new();
			Scores = new();
			CorrectScores = new();
		}

		public SavedScriptState(string savePath) : this()
		{
			SessionId = Guid.NewGuid().ToString();
			SavePath = savePath;
		}

		[JsonIgnore]
		public string SavePath { get; set; }
		[JsonIgnore]
		public Stack<int> InputGroupStack { get; set; }
		public string SessionId { get; set; }
		public Dictionary<string, ScriptDatum> SavedInputs { get; set; }
		public Dictionary<string, object> Variables { get; set; }
		public Dictionary<string, float> Scores { get; set; }
		public Dictionary<string, float> CorrectScores { get; set; }

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

					for (int index = 0; index < value.Count; index++)
					{
						InputGroupStack.Push(value[value.Count - index - 1]);
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
