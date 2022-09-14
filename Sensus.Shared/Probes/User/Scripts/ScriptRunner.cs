// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sensus.Concurrent;
using Sensus.UI.UiProperties;
using Sensus.Context;
using Sensus.Callbacks;
using Newtonsoft.Json;
using Sensus.UI.Inputs;
using Plugin.Permissions.Abstractions;
using System.ComponentModel;
using Sensus.Notifications;
using Sensus.Exceptions;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;
using Xamarin.Forms;
using System.IO;

namespace Sensus.Probes.User.Scripts
{
	public class ScriptRunner : INotifyPropertyChanged, IScriptRunner
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private string _name;
		private bool _enabled;
		private readonly Dictionary<Trigger, Probe.MostRecentDatumChangedDelegateAsync> _triggerHandlers;
		private TimeSpan? _maxAge;
		private readonly List<Tuple<ScheduledCallback, ScriptTriggerTime>> _scheduledCallbackTimes;
		private readonly ScheduleTrigger _scheduleTrigger;
		private bool _hasSubmitted;

		private readonly object _locker = new object();

		/// <summary>
		/// Gets or sets the probe.
		/// </summary>
		/// <value>The probe.</value>
		public ScriptProbe Probe { get; set; }

		/// <summary>
		/// Gets or sets the script.
		/// </summary>
		/// <value>The script.</value>
		public Script Script { get; set; }

		/// <summary>
		/// Gets or sets the saved state
		/// </summary>
		/// <value>The saved state.</value>
		[JsonIgnore] // do not serialize this since it is stored outside of the saved SensusServiceHelper
		public SavedScriptState SavedState { get; set; }

		private string SavedStatePath => Path.Combine(Probe.Protocol.StorageDirectory, "SavedScriptStates");

		private string GetSavedStateFileName()
		{
			return Path.Combine(SavedStatePath, $"{Script.Id}.json");
		}

		private static async Task<bool> PromptForSavedStateAsync(Script script)
		{
			string continuePrompt = script.Runner.ContinuePrompt;

			if (string.IsNullOrWhiteSpace(continuePrompt))
			{
				continuePrompt = "Do you want to Continue from where you stopped previously or Start Over?";
			}

			return await Application.Current.MainPage.DisplayAlert("Continue?", continuePrompt, "Continue", "Start Over");
		}

		public static async Task<SavedScriptState> ManageStateAsync(Script script)
		{
			ScriptRunner runner = script.Runner;

			if (runner.SaveState)
			{
				string savePath = runner.GetSavedStateFileName();

				try
				{
					bool restoreState = runner.SavedState != null || File.Exists(savePath);

					if (restoreState)
					{
						restoreState = await PromptForSavedStateAsync(script);

						if (restoreState)
						{
							if (runner.SavedState == null)
							{
								using (StreamReader reader = new StreamReader(savePath))
								{
									string json = await reader.ReadToEndAsync();

									JsonSerializerSettings settings = new JsonSerializerSettings
									{
										ObjectCreationHandling = ObjectCreationHandling.Replace
									};

									runner.SavedState = JsonConvert.DeserializeObject<SavedScriptState>(json, settings);

									runner.SavedState.SavePath = savePath;
								}
							}

							foreach (KeyValuePair<string, object> pair in runner.SavedState.Variables)
							{
								runner.Probe.Protocol.VariableValue[pair.Key] = pair.Value;
							}

							runner.SavedState.Restored = true;
						}
						else
						{
							ClearSavedState(script);

							foreach (InputGroup inputGroup in script.InputGroups)
							{
								foreach (Input input in inputGroup.Inputs)
								{
									input.Reset();
								}
							}

							runner.SavedState = new SavedScriptState(savePath);
						}
					}
					else
					{
						runner.SavedState = new SavedScriptState(savePath);
					}

					return runner.SavedState;
				}
				catch (Exception e)
				{
					SensusServiceHelper.Get().Logger.Log("Error restoring script saved state:  " + e.Message, LoggingLevel.Normal, typeof(ScriptRunner));

					await SensusServiceHelper.Get().FlashNotificationAsync("Could not restore your previous progress.");

					if (runner.SaveState)
					{
						return new SavedScriptState(savePath);
					}
				}
			}

			return null;
		}

		public static void ClearSavedState(Script script)
		{
			try
			{
				ScriptRunner runner = script.Runner;
				string savePath = runner.GetSavedStateFileName();

				if (File.Exists(savePath))
				{
					File.Delete(savePath);
				}

				runner.SavedState = null;
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get().Logger.Log("Error clearing script saved state:  " + e.Message, LoggingLevel.Normal, typeof(ScriptRunner));
			}
		}

		/// <summary>
		/// Gets the <see cref="IScript"/> that this <see cref="IScriptRunner"/> is configured to run (for NuGet interfacing).
		/// </summary>
		/// <value>The script.</value>
		[JsonIgnore]
		public IScript IScript => Script;

		/// <summary>
		/// Name of the survey. If you would like to use the value of a 
		/// survey-triggering <see cref="Script.CurrentDatum"/> within the survey's name, you can do so 
		/// by placing a <c>{0}</c> within <see cref="Name"/> as a placeholder. The placeholder will be replaced with
		/// the value of the triggering <see cref="Datum"/> at runtime. You can read more about the format of the 
		/// placeholder [here](https://msdn.microsoft.com/en-us/library/system.string.format(v=vs.110).aspx).
		/// </summary>
		/// <value>The name.</value>
		[EntryStringUiProperty("Name:", true, 1, true)]
		public string Name
		{
			get { return _name; }
			set
			{
				if (value != _name)
				{
					_name = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption)));
				}
			}
		}

		/// <summary>
		/// Whether or not the survey is enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Enabled:", true, 2)]
		public bool Enabled
		{
			get
			{
				return _enabled;
			}
			set
			{
				_enabled = value;
			}
		}

		/// <summary>
		/// Whether or not the user should be allowed to cancel the survey after starting it.
		/// </summary>
		/// <value><c>true</c> if allow cancel; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Allow Cancel:", true, 3)]
		public bool AllowCancel { get; set; }

		public ConcurrentObservableCollection<Trigger> Triggers { get; }

		/// <summary>
		/// The maximum number of minutes, following delivery to the user, that this survey should remain available for completion.
		/// </summary>
		/// <value>The max age minutes.</value>
		[EntryDoubleUiProperty("Maximum Age (Mins.):", true, 7, false)]
		public double? MaxAgeMinutes
		{
			get
			{
				return _maxAge?.TotalMinutes;
			}
			set
			{
				_maxAge = value == null ? default(TimeSpan?) : TimeSpan.FromMinutes(value.Value <= 0 ? 10 : value.Value);
			}
		}

		/// <summary>
		/// Whether or not the survey should be removed from availability when the survey's window ends. See <see cref="TriggerWindowsString"/> for more information.
		/// </summary>
		/// <value><c>true</c> if window expiration should be used; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Expire Script When Window Ends:", true, 15)]
		public bool WindowExpiration
		{
			get { return _scheduleTrigger.WindowExpiration; }
			set { _scheduleTrigger.WindowExpiration = value; }
		}

		/// <summary>
		/// 
		/// A comma-separated list of times at (in the case of exact times) or during (in the case of time windows) which the survey should
		/// be delievered. For example, if you want the survey to be delivered twice per day, once randomly between 9am-10am (e.g., 9:32am) 
		/// and once randomly between 1pm-2pm (e.g., 1:56pm), then you would enter the following into this field:
		/// 
		///     9:00-10:00,13:00-14:00
		/// 
		/// Note that the survey will be deployed at a random time during each future window (e.g., 9:32am and 1:56pm on day 1, 9:57am and 
		/// 1:28pm on day 2, etc.). Alternatively, you may specify an exact time as follows:
		/// 
		///     9:00-10:00,11:32,13:00-14:00
		/// 
		/// The survey thus configured will also fire exactly at 11:32am each day. See <see cref="NonDowTriggerIntervalDays"/> 
		/// for how to put additional days between each survey.
		/// 
		/// If you want the survey to be fired on particular days of the week, you can prepend the day of week ("Su", "Mo", "Tu", "We", "Th", 
		/// "Fr", "Sa") to the time as follows:
		/// 
		///     9:00-10:00,Su-11:32,13:00-14:00
		/// 
		/// In contrast to the previous example, this one would would only fire at 11:32am on Sundays.
		/// 
		/// </summary>
		/// <value>The trigger windows string.</value>
		[EntryStringUiProperty("Trigger Windows:", true, 8, false)]
		public string TriggerWindowsString
		{
			get
			{
				return _scheduleTrigger.WindowsString;
			}
			set
			{
				_scheduleTrigger.WindowsString = value;
			}
		}

		/// <summary>
		/// For surveys that are not associated with a specific day of the week, this field indicates how 
		/// many days to should pass between subsequent surveys. For example, if this is set to 1 and 
		/// <see cref="TriggerWindowsString"/> is set to `9:00-10:00`, then the survey would be fired each
		/// day at some time between 9am and 10am. If this field were set to 2, then the survey would be 
		/// fired every other day at some time between 9am and 10am.
		/// </summary>
		/// <value>The non-DOW trigger interval days.</value>
		[EntryIntegerUiProperty("Trigger Interval (Days):", true, 9, true)]
		public int TriggerIntervalDays
		{
			get
			{
				return _scheduleTrigger.TriggerIntervalDays;
			}
			set
			{
				if (value < 1)
				{
					value = 1;
				}

				_scheduleTrigger.TriggerIntervalDays = value;
			}
		}

		/// <summary>
		/// For surveys that are not associated with a specific day of the week, this field indicates how 
		/// many days to should pass between subsequent surveys. For example, if this is set to 1 and 
		/// <see cref="TriggerWindowsString"/> is set to `9:00-10:00`, then the survey would be fired each
		/// day at some time between 9am and 10am. If this field were set to 2, then the survey would be 
		/// fired every other day at some time between 9am and 10am.
		/// </summary>
		/// <value>The non-DOW trigger interval days.</value>
		[EntryIntegerUiProperty("Trigger Interval (Days):", true, 9, true)]
		public bool TriggerIntervalInclusive
		{
			get
			{
				return _scheduleTrigger.TriggerIntervalInclusive;
			}
			set
			{
				_scheduleTrigger.TriggerIntervalInclusive = value;
			}
		}

		/// <summary>
		/// For surveys that are not associated with a specific day of the week, this field indicates how 
		/// many days to should pass between subsequent surveys. For example, if this is set to 1 and 
		/// <see cref="TriggerWindowsString"/> is set to `9:00-10:00`, then the survey would be fired each
		/// day at some time between 9am and 10am. If this field were set to 2, then the survey would be 
		/// fired every other day at some time between 9am and 10am.
		/// </summary>
		/// <value>The non-DOW trigger interval days.</value>
		[EntryIntegerUiProperty("Non-DOW Trigger Interval (Days):", true, 9, true)]
		public int NonDowTriggerIntervalDays
		{
			get
			{
				return _scheduleTrigger.NonDowTriggerIntervalDays;
			}
			set
			{
				if (value < 1)
				{
					value = 1;
				}

				_scheduleTrigger.NonDowTriggerIntervalDays = value;
			}
		}

		[JsonIgnore]
		public string ScheduleTriggerReadableDescription
		{
			get
			{
				string description = _scheduleTrigger.ReadableDescription;

				if (!string.IsNullOrWhiteSpace(description))
				{
					description = char.ToUpper(description[0]) + (description.Length > 1 ? description.Substring(1) : "");
				}

				return description;
			}
		}

		/// <summary>
		/// Property for serialization only. Do not reference due to concurrency issues. Instead, reference
		/// the field <see cref="_scheduledCallbackTimes"/> within a lock statement.
		/// </summary>
		/// <value>The scheduled callbacks.</value>
		[JsonProperty]
		private List<Tuple<ScheduledCallback, ScriptTriggerTime>> ScheduledCallbackTimes
		{
			get
			{
				return _scheduledCallbackTimes;
			}
		}

		/// <summary>
		/// Gets readable descriptions for the scheduled callbacks.
		/// </summary>
		/// <value>The scheduled callback descriptions.</value>
		[JsonIgnore]
		public List<string> ScheduledCallbackDescriptions
		{
			get
			{
				lock (_scheduledCallbackTimes)
				{
					return _scheduledCallbackTimes.Select(scheduledCallbackTime => scheduledCallbackTime.Item1)
												  .Select(scheduledCallback => scheduledCallback.Id + " (" + scheduledCallback.State + "):  " + (scheduledCallback.NextExecution == null ? "Next execution date and time have not been set." : scheduledCallback.NextExecution.ToString()))
												  .ToList();
				}
			}
		}

		/// <summary>
		/// Gets the number of future runs that are currently scheduled.
		/// </summary>
		/// <value>The future run count.</value>
		public int FutureRunCount
		{
			get
			{
				lock (_scheduledCallbackTimes)
				{
					return _scheduledCallbackTimes.Select(scheduledCallbackTime => scheduledCallbackTime.Item1)
												  .Count(scheduledCallback => scheduledCallback.State == ScheduledCallbackState.Scheduled &&
																			  scheduledCallback.NextExecution.GetValueOrDefault(DateTime.MinValue) > DateTime.Now);
				}
			}
		}

		public List<DateTime> RunTimes { get; set; }

		public List<DateTime> CompletionTimes { get; set; }

		public bool HasSubmitted
		{
			get
			{
				return _hasSubmitted;
			}
			set
			{
				bool changed = _hasSubmitted != value;

				_hasSubmitted = value;

				if (changed)
				{
					if (_hasSubmitted)
					{
						SubmissionDate = DateTime.Now;
					}

					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSubmitted)));
				}
			}
		}

		public DateTime? SubmissionDate { get; set; }

		[JsonIgnore]
		public List<DateTime> ScheduledTimes
		{
			get
			{
				return _scheduledCallbackTimes.Select(x => x.Item2.Trigger).ToList();
			}
		}

		/// <summary>
		/// Whether or not to run the survey exactly once.
		/// </summary>
		/// <value><c>true</c> if one shot; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("One Shot:", true, 10)]
		public bool OneShot { get; set; }

		/// <summary>
		/// Whether or not to run the survey immediately upon starting the <see cref="Protocol"/>.
		/// </summary>
		/// <value><c>true</c> if run on start; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Run On Start:", true, 11)]
		public bool RunOnStart { get; set; }

		/// <summary>
		/// Whether or not to display progress (% complete) to the user when they are working on the survey.
		/// </summary>
		/// <value><c>true</c> if display progress; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Display Progress:", true, 13)]
		public bool DisplayProgress { get; set; }

		/// <summary>
		/// How to handle multiple instances of the survey. Options are <see cref="RunMode.Multiple"/>, 
		/// <see cref="RunMode.SingleKeepNewest"/>, and <see cref="RunMode.SingleKeepOldest"/>.
		/// </summary>
		/// <value>The run mode.</value>
		[ListUiProperty("Run Mode:", true, 14, new object[] { RunMode.Multiple, RunMode.SingleKeepNewest, RunMode.SingleKeepOldest }, true)]
		public RunMode RunMode { get; set; }

		/// <summary>
		/// The message to display to the user if a required field is invalid.
		/// </summary>
		/// <value>The incomplete submission confirmation.</value>
		[EntryStringUiProperty("Incomplete Submission Confirmation:", true, 15, false)]
		public string IncompleteSubmissionConfirmation { get; set; }

		/// <summary>
		/// Whether or not to shuffle the order of the survey's <see cref="InputGroup"/>s prior to displaying them to the user. This 
		/// only applies if none of the groups contain <see cref="Input"/>s with display conditions. If display conditions are present, 
		/// then it is not possible to shuffle the <see cref="InputGroup"/>s and this setting will have no effect.
		/// </summary>
		/// <value><c>true</c> if shuffle input groups; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Shuffle Input Groups:", true, 16)]
		public bool ShuffleInputGroups { get; set; }

		/// <summary>
		/// Whether or not to use the triggering <see cref="Datum.Timestamp"/> within the subcaption text
		/// displayed for surveys deployed by this <see cref="ScriptRunner"/>. This is important in scenarios
		/// where <see cref="Script.Birthdate"/> differs from <see cref="Datum.Timestamp"/> (e.g., as is the
		/// case in iOS where readings collected by the activity probe lag by several minutes).
		/// </summary>
		/// <value><c>true</c> if use trigger datum timestamp in subcaption; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Use Trigger Timestamp In Subcaption:", true, 17)]
		public bool UseTriggerDatumTimestampInSubcaption { get; set; }

		/// <summary>
		/// Whether or not to force a local-to-remote transfer to run each time this survey is submitted
		/// by the user.
		/// </summary>
		/// <value><c>true</c> to force transfer on submission; otherwise, <c>false</c>.</value>
		[OnOffUiProperty("Force Remote Storage On Submission:", true, 18)]
		public bool ForceRemoteStorageOnSurveySubmission { get; set; }

		/// <summary>
		/// Tolerance in milliseconds for running the <see cref="Script"/> before the scheduled 
		/// time, if doing so will increase the number of batched actions and thereby decrease battery consumption.
		/// </summary>
		/// <value>The delay tolerance before.</value>
		[EntryIntegerUiProperty("Delay Tolerance Before (MS):", true, 19, true)]
		public int DelayToleranceBeforeMS { get; set; }

		/// <summary>
		/// Tolerance in milliseconds for running the <see cref="Script"/> after the scheduled 
		/// time, if doing so will increase the number of batched actions and thereby decrease battery consumption.
		/// </summary>
		/// <value>The delay tolerance before.</value>
		[EntryIntegerUiProperty("Delay Tolerance After (MS):", true, 20, true)]
		public int DelayToleranceAfterMS { get; set; }

		[OnOffUiProperty("Allow User Initiation:", true, 21)]
		public bool AllowUserInitiation { get; set; }

		[EntryStringUiProperty("Script Group:", true, 22, false)]
		public string ScriptGroup { get; set; }

		[OnOffUiProperty("Keep Until Completed:", true, 23)]
		public bool KeepUntilCompleted { get; set; }

		[ScriptsUiProperty("Next Script:", true, 24, false)]
		public ScriptRunner NextScript { get; set; }

		[EntryStringUiProperty("Next Script Run Delay (MS):", true, 25, false)]
		public int NextScriptRunDelayMS { get; set; }

		[OnOffUiProperty("Trigger Next Script First Time Only:", true, 26)]
		public bool TriggerNextScriptFirstTimeOnly { get; set; }

		[ScriptsUiProperty("Required Scripts:", true, 26, false)]
		public List<ScriptRunner> RequiredScripts { get; set; }

		[OnOffUiProperty("Confirm Navigation:", true, 27)]
		public bool ConfirmNavigation { get; set; }

		[OnOffUiProperty("Use Detail Page:", true, 28)]
		public bool UseDetailPage { get; set; }

		[OnOffUiProperty("Save State:", true, 29)]
		public bool SaveState { get; set; }

		[EntryStringUiProperty("Continue Prompt:", true, 30, false)]
		public string ContinuePrompt { get; set; }

		[EntryStringUiProperty("Submit Confirmation:", true, 31, false)]
		public string SubmitConfirmation { get; set; }

		[EntryStringUiProperty("Reminder Intervals (S):", true, 32, false)]
		public string ReminderIntervals { get; set; }

		[EntryStringUiProperty("Reminder Message:", true, 33, false)]
		public string ReminderMessage { get; set; }

		[EntryStringUiProperty("Notification Message:", true, 34, false)]
		public string NotificationMessage { get; set; }

		[JsonIgnore]
		public string Caption
		{
			get
			{
				return _name;
			}
		}

		private ScriptRunner()
		{
			_scheduleTrigger = new ScheduleTrigger(); //this needs to be above
			_enabled = false;
			_maxAge = null;
			_triggerHandlers = new Dictionary<Trigger, Probe.MostRecentDatumChangedDelegateAsync>();
			_scheduledCallbackTimes = new List<Tuple<ScheduledCallback, ScriptTriggerTime>>();
			Script = new Script(this);
			Triggers = new ConcurrentObservableCollection<Trigger>(new LockConcurrent());
			RunTimes = new List<DateTime>();
			CompletionTimes = new List<DateTime>();
			AllowCancel = true;
			OneShot = false;
			RunOnStart = false;
			DisplayProgress = true;
			RunMode = RunMode.SingleKeepNewest;
			IncompleteSubmissionConfirmation = "You have not completed all required fields on the current page. Do you want to continue?";
			ShuffleInputGroups = false;
			ConfirmNavigation = true;
			SubmitConfirmation = "Are you ready to submit your responses?";
			ReminderCallbackIds = new List<string>();

			RequiredScripts = new List<ScriptRunner>();

			Triggers.CollectionChanged += (o, e) =>
			{
				if (e.Action == NotifyCollectionChangedAction.Add)
				{
					foreach (Trigger trigger in e.NewItems)
					{
						// ignore duplicate triggers -- the user should delete and re-add them instead.
						if (_triggerHandlers.ContainsKey(trigger))
						{
							return;
						}

						// create a handler to be called each time the triggering probe stores a datum
						Probe.MostRecentDatumChangedDelegateAsync handler = async (previousDatum, currentDatum) =>
						{
							// must be running and must have a current datum
							lock (_locker)
							{
								if (Probe.State != ProbeState.Running || !_enabled || currentDatum == null)
								{
									trigger.FireValueConditionMetOnPreviousCall = false;  // this covers the case when the current datum is null. for some probes, the null datum is meaningful and is emitted in order for their state to be tracked appropriately (e.g., POI probe).
									return;
								}
							}

							// get the value that might trigger the script -- it might be null in the case where the property is nullable and is not set (e.g., input locations, etc.)
							object currentDatumValue = trigger.DatumProperty.GetValue(currentDatum);
							if (currentDatumValue == null)
							{
								return;
							}

							// if we're triggering based on datum value changes/differences instead of absolute values, calculate the change now.
							if (trigger.Change)
							{
								// don't need to set ConditionSatisfiedLastTime = false here, since it cannot be the case that it's true and prevDatum == null (we must have had a currDatum last time in order to set ConditionSatisfiedLastTime = true).
								if (previousDatum == null)
								{
									return;
								}

								try
								{
									currentDatumValue = Convert.ToDouble(currentDatumValue) - Convert.ToDouble(trigger.DatumProperty.GetValue(previousDatum));
								}
								catch (Exception ex)
								{
									SensusServiceHelper.Get().Logger.Log("Trigger error:  Failed to convert datum values to doubles for change calculation:  " + ex.Message, LoggingLevel.Normal, GetType());
									return;
								}
							}

							// run the script if the current datum's value satisfies the trigger
							if (trigger.FireFor(currentDatumValue))
							{
								await RunAsync(Script.Copy(true), previousDatum, currentDatum);
							}
						};

						trigger.Probe.MostRecentDatumChanged += handler;

						_triggerHandlers.Add(trigger, handler);
					}
				}
				else if (e.Action == NotifyCollectionChangedAction.Remove)
				{
					foreach (Trigger trigger in e.OldItems)
					{
						if (_triggerHandlers.ContainsKey(trigger))
						{
							trigger.Probe.MostRecentDatumChanged -= _triggerHandlers[trigger];

							_triggerHandlers.Remove(trigger);
						}
					}
				}
			};
		}

		public ScriptRunner(string name, ScriptProbe probe) : this()
		{
			Name = name;
			Probe = probe;
		}

		public async Task InitializeAsync()
		{
			foreach (Trigger trigger in Triggers)
			{
				trigger.Reset();
			}

			// ensure all variables defined by inputs are listed on the protocol
			List<string> unknownVariables = Script.InputGroups.SelectMany(inputGroup => inputGroup.Inputs)
															  .OfType<IVariableDefiningInput>()
															  .Where(input => !string.IsNullOrWhiteSpace(input.DefinedVariable) && input.DefinedVariable.StartsWith('!') == false)
															  .Select(input => input.DefinedVariable)
															  .Where(definedVariable => !Probe.Protocol.VariableValue.ContainsKey(definedVariable))
															  .ToList();
			if (unknownVariables.Count > 0)
			{
				throw new Exception("The following input-defined variables are not listed on the protocol:  " + string.Join(", ", unknownVariables));
			}

			if (Script.InputGroups.Any(inputGroup => inputGroup.Geotag))
			{
				await SensusServiceHelper.Get().ObtainPermissionAsync(Permission.Location);
			}
		}

		public async Task StartAsync()
		{
			if (RequiredScripts == null)
			{
				RequiredScripts = new();
			}

			await ScheduleScriptRunsAsync();

			// use the async version below for a couple reasons. first, we're in a non-async method and we want to ensure
			// that the script won't be running on the UI thread. second, from the caller's perspective the prompt should 
			// not need to finish running in order for the runner to be considered started.
			if (RunOnStart)
			{
				await RunAsync(Script.Copy(true));
			}
		}

		public async Task ResetAsync()
		{
			await UnscheduleCallbacksAsync();
			HasSubmitted = false;
			SubmissionDate = null;
			RunTimes.Clear();
			CompletionTimes.Clear();
			SavedState = null;
		}

		public async Task RestartAsync()
		{
			await StopAsync();
			await StartAsync();
		}

		public async Task StopAsync()
		{
			await UnscheduleCallbacksAsync();

			await UnscheduleRemindersAsync();

			if (SensusServiceHelper.Get().RemoveScriptsForRunner(this))
			{
				await SensusServiceHelper.Get().IssuePendingSurveysNotificationAsync(PendingSurveyNotificationMode.None, Probe.Protocol);
			}

			try
			{
				Directory.Delete(SavedStatePath, true);
			}
			catch (Exception e)
			{
				SensusServiceHelper.Get().Logger.Log($"Failed to delete saved state: {e.Message}", LoggingLevel.Normal, GetType());
			}
		}

		private static bool IsPastTrigger(ScriptTriggerTime triggerTime)
		{
			return triggerTime.Trigger <= DateTime.Now && (triggerTime.Expiration == null || triggerTime.Expiration > DateTime.Now);
		}

		public async Task ScheduleScriptRunsAsync()
		{
			if (
				SensusServiceHelper.Get() == null ||               // the service helper hasn't loaded
				(_scheduleTrigger.WindowCount == 0 && _scheduledCallbackTimes.Count == 0) ||               // there are no windows to schedule and no scheduled callback times
				Probe == null ||                                   // there is no probe
				Probe.Protocol.State == ProtocolState.Stopped ||   // protocol has stopped
				Probe.Protocol.State == ProtocolState.Stopping ||  // protocol is about to stop
				!_enabled)                                         // script is disabled
			{
				return;
			}

			//bool isPastTrigger(ScriptTriggerTime x) => x.Trigger <= DateTime.Now && (x.Expiration == null || x.Expiration > DateTime.Now);

			// clean up scheduled callback times
			List<ScriptTriggerTime> callbackTimesToReschedule = new List<ScriptTriggerTime>();
			lock (_scheduledCallbackTimes)
			{
				// remove any callbacks whose times have passed. be sure to use the callback's next execution time, as this may differ
				// from the script trigger time when callback batching is enabled. for example, if the callback permit delay tolerance
				// prior to the trigger time, then the scheduled callback next execution time may precede the trigger time. 
				//_scheduledCallbackTimes.RemoveAll(scriptRunCallback => scriptRunCallback.Item1.NextExecution.GetValueOrDefault(DateTime.MinValue) < DateTime.Now);

				// get future callbacks that need to be rescheduled. it can happen that the app crashes or is killed 
				// and then resumes (e.g., on ios push notification), and it's important for the times of the scheduled 
				// callbacks to be maintained. this is particularly important for scheduled times that are very far out 
				// in the future. if the app is restarted more often than the script is run, then the script will likely 
				// never be run.
				foreach (Tuple<ScheduledCallback, ScriptTriggerTime> callbackTime in _scheduledCallbackTimes.ToList())
				{
					if (!SensusContext.Current.CallbackScheduler.ContainsCallback(callbackTime.Item1))
					{
						// it's correct to reference the trigger time rather than the callback time, as the former
						// is what should be used in callback batching.
						callbackTimesToReschedule.Add(callbackTime.Item2);

						// we're about to reschedule the callback. remove the old one so we don't have duplicates.
						_scheduledCallbackTimes.Remove(callbackTime);
					}
					else if (IsPastTrigger(callbackTime.Item2))
					{
						// the trigger time is in the past but it is also in SensusContext.Current.CallbackScheduler, so it should not need to be run or rescheduled
						_scheduledCallbackTimes.Remove(callbackTime);
					}
				}
			}

			DateTime? recentRunDate = null;

			// if there are any trigger times in the past, then make sure the script has been run
			if (callbackTimesToReschedule.OrderByDescending(x => x.Trigger).FirstOrDefault(IsPastTrigger) is ScriptTriggerTime pastTriggerTime)
			{
				// store this recent run date to prevent scheduling of another trigger within the same window down below
				recentRunDate = DateTime.Now;

				await RunAsync(Script.Copy(true, pastTriggerTime));
			}

			callbackTimesToReschedule.RemoveAll(x => x.Trigger <= DateTime.Now);

			// reschedule script runs as needed
			foreach (ScriptTriggerTime callbackTimeToReschedule in callbackTimesToReschedule)
			{
				await ScheduleScriptRunAsync(callbackTimeToReschedule);
			}

			// abort if we already have enough runs scheduled in the future. only allow a maximum of 32 script-run callbacks 
			// to be scheduled app-wide leaving room for other callbacks (e.g., the storage and polling systems). android's 
			// app-level limit is 500, and ios 9 has a limit of 64. not sure about ios 10+.
			int scriptRunCallbacksForThisRunner = (32 / Probe.ScriptRunners.Count);
			scriptRunCallbacksForThisRunner = Math.Max(scriptRunCallbacksForThisRunner, 1);  // schedule at least 1 run, regardless of the os cap.
			scriptRunCallbacksForThisRunner = Math.Min(scriptRunCallbacksForThisRunner, 3);  // cap the runs, as each one takes some time to schedule.
			lock (_scheduledCallbackTimes)
			{
				if (_scheduledCallbackTimes.Count >= scriptRunCallbacksForThisRunner)
				{
					return;
				}
			}

			DateTime baseIntervalDate = Probe.Protocol.InstallDate;

			if (RequiredScripts.Any())
			{
				if (RequiredScripts.All(x => x.SubmissionDate != null))
				{
					baseIntervalDate = RequiredScripts.Max(x => x.SubmissionDate) ?? Probe.Protocol.InstallDate;
				}
				else
				{
					return;
				}
			}

			// get all trigger times starting from today
			foreach (ScriptTriggerTime triggerTime in _scheduleTrigger.GetTriggerTimes(DateTime.Now, baseIntervalDate, _maxAge))
			{
				// schedule all future runs, except those beyond the protocol's end date (if there is one). need to check
				// that they're in the future because GetTriggerTimes will return all times starting from 0:00 of the current
				// day.

				TriggerWindow window = new TriggerWindow(triggerTime.Window);
				DateTime windowStart = triggerTime.Trigger.Date.Add(window.Start);
				DateTime windowEnd = triggerTime.Trigger.Date.Add(window.End);
				bool alreadyRanInWindow = recentRunDate != null && windowStart <= recentRunDate && recentRunDate <= windowEnd;

				if (alreadyRanInWindow == false && triggerTime.Trigger > DateTime.Now && (Probe.Protocol.ContinueIndefinitely || triggerTime.Trigger < Probe.Protocol.EndDate))
				{
					await ScheduleScriptRunAsync(triggerTime);

					// stop when we have scheduled enough runs
					lock (_scheduledCallbackTimes)
					{
						if (_scheduledCallbackTimes.Count >= scriptRunCallbacksForThisRunner)
						{
							break;
						}
					}
				}
			}

			// save the app state to retain the callback schedule in case the app terminates
			await SensusServiceHelper.Get().SaveAsync();
		}

		/// <summary>
		/// Schedules a script run. 
		/// </summary>
		/// <returns>Task.</returns>
		/// <param name="triggerTime">Trigger time.</param>
		/// <param name="scriptId">Script identifier. If null, a random identifier will be generated for the script to run.</param>
		private async Task ScheduleScriptRunAsync(ScriptTriggerTime triggerTime, string scriptId = null)
		{
			ScheduledCallback callback = CreateScriptRunCallback(triggerTime, scriptId);

			// there is a race condition, so far only seen in ios, in which multiple script runner notifications
			// accumulate and are executed concurrently when the user opens the app. when these script runners
			// execute they add their scripts to the pending scripts collection and concurrently attempt to 
			// schedule all future scripts. because two such attempts are made concurrently, they may race to 
			// schedule the same future script. each script callback id is functional in the sense that it is
			// a string denoting the script to run and the time window to run within. thus, the callback ids can
			// duplicate. the callback scheduler checks for such duplicate ids and will return unscheduled on the next
			// line when a duplicate is detected. in the case of a duplicate we can simply abort scheduling the
			// script run since it was already scheduled. this issue is much less common in android because all 
			// scripts are run immediately in the background, producing little opportunity for the race condition.
			if (await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(callback) == ScheduledCallbackState.Scheduled)
			{
				lock (_scheduledCallbackTimes)
				{
					_scheduledCallbackTimes.Add(new Tuple<ScheduledCallback, ScriptTriggerTime>(callback, triggerTime));
				}

				SensusServiceHelper.Get().Logger.Log($"Scheduled for {triggerTime.Trigger} ({callback.Id})", LoggingLevel.Normal, GetType());
			}
		}

		private async Task ScheduleScriptRunAsync(DateTime triggerDateTime, string scriptId = null)
		{
			ScriptTriggerTime triggerTime = new ScriptTriggerTime(triggerDateTime, null, "");

			await ScheduleScriptRunAsync(triggerTime, scriptId);
		}

		public async Task<bool> ScheduleScriptFromInputAsync(Script script)
		{
			IEnumerable<ScriptSchedulerInput> schedulers = script.InputGroups.SelectMany(x => x.Inputs).OfType<ScriptSchedulerInput>();
			bool scheduledNext = false;
			bool save = schedulers.Any();

			foreach (ScriptSchedulerInput scheduler in schedulers)
			{
				try
				{
					if (scheduler.Complete && scheduler.Value is DateTime scheduledTime)
					{
						ScriptRunner runner = scheduler.ScheduledScript;

						if (scheduler.ScheduleMode == ScheduleModes.Self)
						{
							runner = this;
						}
						else if (scheduler.ScheduleMode == ScheduleModes.Next && NextScript != null)
						{
							runner = NextScript;
						}
						else if (runner == null)
						{
							if (scheduler.ScheduleMode == ScheduleModes.Reminder)
							{
								if (string.IsNullOrWhiteSpace(scheduler.ScriptGroup) == false)
								{
									runner = SensusServiceHelper.Get().ScriptsToRun.LastOrDefault(x => x.Runner.ScriptGroup == scheduler.ScriptGroup)?.Runner;
								}
								else
								{
									runner = SensusServiceHelper.Get().ScriptsToRun.LastOrDefault()?.Runner;
								}
							}
						}

						if (runner != null)
						{
							if (scheduler.TimeOnly)
							{
								int daysFromNow = 0;

								if (scheduler.DaysInFuture > 0)
								{
									daysFromNow = scheduler.DaysInFuture;
								}
								else if (scheduledTime.TimeOfDay < DateTime.Now.TimeOfDay)
								{
									daysFromNow = 1;
								}

								scheduledTime = DateTime.Now.Date.AddDays(daysFromNow).Add(scheduledTime.TimeOfDay);
							}

							if (scheduler.ScheduleMode == ScheduleModes.Reminder)
							{
								await runner.UnscheduleRemindersAsync();

								await runner.ScheduleReminderAsync(runner.Script, scheduledTime, scheduler.NotificationMessage);
							}
							else
							{
								await runner.ScheduleScriptRunAsync(scheduledTime);

								scheduledNext = runner == NextScript;
							}
						}
					}
				}
				catch (Exception e)
				{
					SensusServiceHelper.Get().Logger.Log($"Failed to schedule from SchedulerInput with {scheduler.ScheduleMode}: {e.Message}", LoggingLevel.Normal, GetType());
				}
			}

			if (save)
			{
				await SensusServiceHelper.Get().SaveAsync();
			}

			return scheduledNext;
		}

		public async Task ScheduleNextScriptToRunAsync()
		{
			if (NextScript != null && NextScript.Enabled && (TriggerNextScriptFirstTimeOnly == false || HasSubmitted == false))
			{
				// if there is no window then run it immmediately
				if (NextScriptRunDelayMS == 0)
				{
					await NextScript.RunAsync(NextScript.Script.Copy(true));
				}
				else // schedule it to be run
				{
					await NextScript.ScheduleScriptRunAsync(DateTime.Now.AddMilliseconds(NextScriptRunDelayMS));

					await SensusServiceHelper.Get().SaveAsync();
				}
			}
		}

		public async Task ScheduleDepedentScriptsAsync()
		{
			IEnumerable<ScriptRunner> dependents = Probe.ScriptRunners.Where(x => x.Enabled && x.RequiredScripts != null && x.RequiredScripts.Contains(this));

			if (dependents.Any())
			{
				foreach (ScriptRunner runner in dependents)
				{
					await runner.ScheduleScriptRunsAsync();
				}

				await SensusServiceHelper.Get().SaveAsync();
			}
		}

		public List<string> ReminderCallbackIds { get; set; }

		private async Task ScheduleReminderAsync(Script script, DateTimeOffset startDate, TimeSpan timeSpan, bool repeats, string notificationMessage)
		{
			if (startDate > DateTimeOffset.Now || repeats)
			{
				string id = $"{Script.Id}.{GetType().FullName}.Reminder";

				if (repeats && timeSpan > TimeSpan.Zero)
				{
					id += $".{DateTime.Now:s}+";
				}

				id += timeSpan.ToString();

				if (SensusContext.Current.CallbackScheduler.ContainsCallback(id) == false)
				{
					ScheduledCallback callback = new ScheduledCallback(c =>
					{
						Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Reminded, DateTimeOffset.Now, script), CancellationToken.None);

						return Task.CompletedTask;
					}, startDate - DateTimeOffset.Now, id, Probe.Protocol.Id, Probe.Protocol, null, TimeSpan.FromMilliseconds(DelayToleranceBeforeMS), TimeSpan.FromMilliseconds(DelayToleranceAfterMS), ScheduledCallbackPriority.High, GetType());

					if (repeats && timeSpan > TimeSpan.Zero)
					{
						callback.RepeatDelay = timeSpan;

						callback.RepeatPredicate = () => script.Runner.Probe.State == ProbeState.Running;
					}

					if (string.IsNullOrWhiteSpace(notificationMessage) == false)
					{
						callback.UserNotificationMessage = notificationMessage;
					}
					else if (string.IsNullOrWhiteSpace(script.Runner.ReminderMessage) == false)
					{
						callback.UserNotificationMessage = script.Runner.ReminderMessage;
					}
					else
					{
						callback.UserNotificationMessage = $"{script.Runner.Name} is ready in your Surveys list. Click here to view it.";
					}

					callback.NotificationUserResponseAction = NotificationUserResponseAction.DisplayPendingSurveys;

					if (await SensusContext.Current.CallbackScheduler.ScheduleCallbackAsync(callback) == ScheduledCallbackState.Scheduled)
					{
						if (ReminderCallbackIds.Contains(id) == false)
						{
							ReminderCallbackIds.Add(id);
						}

						SensusServiceHelper.Get().Logger.Log($"Scheduled {timeSpan} second reminder for {script.Id}", LoggingLevel.Normal, GetType());
					}
					else
					{
						SensusServiceHelper.Get().Logger.Log($"Could not schedule {timeSpan} second reminder for {script.Id}", LoggingLevel.Normal, GetType());
					}
				}
			}
		}
		public async Task ScheduleReminderAsync(Script script, DateTime dateTime, string notificationMessage)
		{
			await ScheduleReminderAsync(script, dateTime, TimeSpan.Zero, false, notificationMessage);
		}
		public async Task ScheduleRemindersAsync(Script script, string notificationMessage = null)
		{
			if (script.RunTime != null && string.IsNullOrWhiteSpace(script.Runner.ReminderIntervals) == false)
			{
				IEnumerable<string> intervalStrings = script.Runner.ReminderIntervals.Split(",").Select(x => x.Trim());

				foreach (string intervalString in intervalStrings)
				{
					bool repeats = false;
					string parsableInterval = intervalString;

					if (intervalString.EndsWith("*"))
					{
						parsableInterval = intervalString[0..^1];

						repeats = true;
					}

					TimeSpan timeSpan = TimeSpan.Zero;
					DateTimeOffset dateTime = script.RunTime.Value.Add(timeSpan);

					if (int.TryParse(parsableInterval, out int interval))
					{
						timeSpan = TimeSpan.FromSeconds(interval);

						await ScheduleReminderAsync(script, dateTime, timeSpan, repeats, notificationMessage);
					}
					else if (parsableInterval.Contains(":") && TimeSpan.TryParse(parsableInterval, out timeSpan))
					{
						await ScheduleReminderAsync(script, dateTime, timeSpan, repeats, notificationMessage);
					}
				}
			}
		}

		private async Task UnscheduleReminderAsync(string id)
		{
			await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(id);

			lock (ReminderCallbackIds)
			{
				ReminderCallbackIds.Remove(id);
			}
		}
		public async Task UnscheduleRemindersAsync()
		{
			foreach (string id in ReminderCallbackIds.ToList())
			{
				await UnscheduleReminderAsync(id);
			}
		}

		/// <summary>
		/// Creates the script run callback.
		/// </summary>
		/// <returns>The script run callback.</returns>
		/// <param name="triggerTime">Trigger time.</param>
		/// <param name="scriptId">Script identifier. If null, then a random identifier will be generated for the script that will be run.</param>
		private ScheduledCallback CreateScriptRunCallback(ScriptTriggerTime triggerTime, string scriptId = null)
		{
			Script scriptToRun = Script.Copy(true, triggerTime);

			// if we're passed a run ID, then override the random one that was generated above in the call to Script.Copy.
			if (scriptId != null)
			{
				scriptToRun.Id = scriptId;
			}

			ScheduledCallback callback = new ScheduledCallback(async cancellationToken =>
			{
				SensusServiceHelper.Get().Logger.Log("Running script \"" + Name + "\".", LoggingLevel.Normal, GetType());

				if (Probe.State != ProbeState.Running || !_enabled)
				{
					return;
				}

				await RunAsync(scriptToRun);

				// on android, the callback alarm has fired and the script has been run. on ios, the notification has been
				// delivered (1) to the app in the foreground, (2) to the notification tray where the user has opened
				// it, or (3) via push notification in the background. in any case, the script has been run. now is a good 
				// time to update the scheduled callbacks to run this script.
				await ScheduleScriptRunsAsync();

			}, triggerTime.TimeTillTrigger, Script.Id + "." + GetType().FullName + "." + (triggerTime.Trigger - DateTime.MinValue).Days + "." + triggerTime.Window, Probe.Protocol.Id, Probe.Protocol, null, TimeSpan.FromMilliseconds(DelayToleranceBeforeMS), TimeSpan.FromMilliseconds(DelayToleranceAfterMS), ScheduledCallbackPriority.High, GetType());  // use Script.Id rather than script.Id for the callback identifier. using the former means that callbacks are unique to the script runner and not the script copies (the latter) that we will be running. the latter would always be unique.

#if __IOS__
			// all scheduled scripts with an expiration should show an expiration date to the user. on iOS this will be the only notification for 
			// scheduled surveys, since we don't have a way to update the "you have X pending surveys" notification (generated by triggered 
			// surveys) without executing code in the background.
			if (scriptToRun.ExpirationDate.HasValue)
			{
				callback.UserNotificationMessage = "Survey expires on " + scriptToRun.ExpirationDate.Value.ToShortDateString() + " at " + scriptToRun.ExpirationDate.Value.ToShortTimeString() + ".";
			}
			// on iOS, even if we don't have an expiration date we should show some additional notification, again because we don't have a way
			// to update the "you have X pending surveys" notification from the background.
			else
			{
				callback.UserNotificationMessage = "Please open to take survey.";
			}

			callback.NotificationUserResponseAction = NotificationUserResponseAction.DisplayPendingSurveys;
#endif

			return callback;
		}

		private async Task UnscheduleCallbacksAsync()
		{
			List<ScheduledCallback> callbacksToUnschedule = new List<ScheduledCallback>();

			lock (_scheduledCallbackTimes)
			{
				if (_scheduledCallbackTimes.Count == 0 || SensusServiceHelper.Get() == null)
				{
					return;
				}

				callbacksToUnschedule = _scheduledCallbackTimes.Select(scheduledCallbackTime => scheduledCallbackTime.Item1).ToList();

				_scheduledCallbackTimes.Clear();
			}

			foreach (ScheduledCallback callbackToUnschedule in callbacksToUnschedule)
			{
				await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(callbackToUnschedule);
			}
		}

		public async Task RunAsync(Script script, Datum previousDatum = null, Datum currentDatum = null)
		{
			SensusServiceHelper.Get().Logger.Log($"Running \"{Name}\".", LoggingLevel.Normal, GetType());

			script.RunTime = DateTimeOffset.UtcNow;

			await ScheduleRemindersAsync(script);

			// this method can be called with previous / current datum values (e.g., when the script is first triggered). it 
			// can also be called without previous / current datum values (e.g., when triggering on a schedule). if
			// we have such values, set them on the script.

			if (previousDatum != null)
			{
				script.PreviousDatum = previousDatum;
			}

			if (currentDatum != null)
			{
				script.CurrentDatum = currentDatum;
			}

			// scheduled scripts have their expiration dates set when they're scheduled. scripts triggered by other probes
			// as well as on-start scripts will not yet have their expiration dates set. so check the script we've been 
			// given and set the expiration date if needed. triggered scripts don't have windows, so the only expiration
			// condition comes from the maximum age.
			if (script.ExpirationDate == null && _maxAge.HasValue)
			{
				script.ExpirationDate = script.Birthdate + _maxAge.Value;
			}

			// script could have already expired (e.g., if user took too long to open notification).
			if (script.ExpirationDate.HasValue && script.ExpirationDate.Value < DateTime.Now)
			{
				SensusServiceHelper.Get().Logger.Log("Script expired before it was run.", LoggingLevel.Normal, GetType());
				return;
			}

			// do not run a one-shot script if it has already been run
			if (OneShot && RunTimes.Count > 0)
			{
				SensusServiceHelper.Get().Logger.Log("Not running one-shot script multiple times.", LoggingLevel.Normal, GetType());
				return;
			}

			// check with the survey agent if there is one
			if (Probe.Agent != null)
			{
				Tuple<bool, DateTimeOffset?> deliverFutureTime = await Probe.Agent.DeliverSurveyNowAsync(script);

				if (deliverFutureTime.Item1)
				{
					Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.AgentAccepted, script.RunTime.Value, script), CancellationToken.None);
				}
				else
				{
					if (deliverFutureTime.Item2 == null)
					{
						SensusServiceHelper.Get().Logger.Log("Agent has declined survey without deferral.", LoggingLevel.Normal, GetType());

						Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.AgentDeclined, script.RunTime.Value, script), CancellationToken.None);
					}
					else if (deliverFutureTime.Item2.Value > DateTimeOffset.UtcNow)
					{
						SensusServiceHelper.Get().Logger.Log("Agent has deferred survey until:  " + deliverFutureTime.Item2.Value, LoggingLevel.Normal, GetType());

						Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.AgentDeferred, script.RunTime.Value, script), CancellationToken.None);

						// check whether we need to expire the rescheduled script at some future point
						DateTime? expiration = null;
						DateTime trigger = deliverFutureTime.Item2.Value.LocalDateTime;
						if (_maxAge.HasValue)
						{
							expiration = trigger + _maxAge.Value;
						}

						// there is no window, so just add a descriptive, unique descriptor in place of the window
						ScriptTriggerTime triggerTime = new ScriptTriggerTime(trigger, expiration, "DEFERRED-" + Guid.NewGuid());

						// schedule the trigger. since this is a deferral, use the same script identifier that we currently have. this 
						// will maintain consistency and interpretability of the ScriptStateDatum objects that are recording the progression
						// of scripts. this will also let survey agents better interpret what's going on with deferrals. this identifier is
						// used as the RunId in the various tracked data types.
						await ScheduleScriptRunAsync(triggerTime, script.Id);
					}
					else
					{
						SensusServiceHelper.Get().Logger.Log("Warning:  Agent has deferred survey to a time in the past:  " + deliverFutureTime.Item2.Value, LoggingLevel.Normal, GetType());
					}

					// do not proceed. the calling method (if scheduler-based) will take care of removing the current script.
					return;
				}
			}

			lock (RunTimes)
			{
				// track participation by recording the current time. use this instead of the script's run timestamp, since
				// the latter is the time of notification on ios rather than the time that the user actually viewed the script.
				RunTimes.Add(DateTime.Now);
				RunTimes.RemoveAll(r => r < Probe.Protocol.ParticipationHorizon);
			}

			await SensusServiceHelper.Get().AddScriptAsync(script, RunMode);

			// let the script agent know and store a datum to record the event
			await (Probe.Agent?.ObserveAsync(script, ScriptState.Delivered) ?? Task.CompletedTask);
			Probe.Protocol.LocalDataStore.WriteDatum(new ScriptStateDatum(ScriptState.Delivered, script.RunTime.Value, script), CancellationToken.None);
		}

		private string GetCopyName()
		{
			try
			{
				string pattern = @"\s*-\s*Copy\s*(?<number>\d*)$";

				string name = Regex.Replace(Name, pattern, "");

				string countString = Probe.ScriptRunners.Max(x => Regex.Match(x.Name, $@"(?<={name}){pattern}").Groups["number"].Value);

				int.TryParse(countString, out int count);

				count = Math.Max(count, Probe.ScriptRunners.Count(x => Regex.IsMatch(x.Name, $@"(?<={name})({pattern})?$")) - 1);

				if (count > 0)
				{
					name += $" - Copy {count + 1}";
				}
				else
				{
					name += " - Copy";
				}

				return name;
			}
			catch
			{
				SensusServiceHelper.Get().Logger.Log("Error creating unique script runner name", LoggingLevel.Normal, GetType());
			}

			return Name + " - Copy";
		}

		public ScriptRunner Copy()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				PreserveReferencesHandling = PreserveReferencesHandling.None,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				TypeNameHandling = TypeNameHandling.All,
				ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
			};

			try
			{
				SensusServiceHelper.Get().FlashNotificationsEnabled = false;
				ScriptRunner copy = JsonConvert.DeserializeObject<ScriptRunner>(JsonConvert.SerializeObject(this, settings), settings);

				copy.Script.Id = Guid.NewGuid().ToString();
				copy.Probe = Probe;

				copy.Name = GetCopyName();
				copy.HasSubmitted = false;

				copy.SavedState = null;

				foreach (InputGroup inputGroup in copy.Script.InputGroups)
				{
					inputGroup.Id = Guid.NewGuid().ToString();
				}

				return copy;
			}
			catch (Exception ex)
			{
				string message = $"Failed to copy script runner:  {ex.Message}";
				SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
				SensusException.Report(message, ex);
				return null;
			}
			finally
			{
				SensusServiceHelper.Get().FlashNotificationsEnabled = true;
			}
		}
	}
}
