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
using Sensus.Extensions;
using Sensus.UI.UiProperties;
using Sensus.Context;
using Sensus.Callbacks;
using Newtonsoft.Json;
using Sensus.UI.Inputs;
using Plugin.Permissions.Abstractions;
using System.ComponentModel;

#if __IOS__
using Sensus.Notifications;
#endif

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

        private readonly object _locker = new object();

        public ScriptProbe Probe { get; set; }

        public Script Script { get; set; }

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
                if(value != _name)
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
        /// For serialization only. Do not reference due to concurrency issues.
        /// </summary>
        /// <value>The scheduled callbacks.</value>
        public List<Tuple<ScheduledCallback, ScriptTriggerTime>> ScheduledCallbackTimes
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
                return _scheduledCallbackTimes.Select(scheduledCallbackTime => scheduledCallbackTime.Item1)
                                              .Count(scheduledCallback => scheduledCallback.State == ScheduledCallbackState.Scheduled &&
                                                                          scheduledCallback.NextExecution.GetValueOrDefault(DateTime.MinValue) > DateTime.Now);
            }
        }

        public List<DateTime> RunTimes { get; set; }

        public List<DateTime> CompletionTimes { get; set; }

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
        /// Whether or not to shuffle the order of the survey's input groups prior to displaying them to the user.
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
        /// Whether or not to force a local-to-remote transfer to run each time this survey is completed
        /// by the user.
        /// </summary>
        /// <value><c>true</c> to force transfer on survey submission; otherwise, <c>false</c>.</value>
        [OnOffUiProperty("Force Remote Storage On Survey Submission:", true, 18)]
        public bool ForceRemoteStorageOnSurveySubmission { get; set; }

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
                                if (!Probe.Running || !_enabled || currentDatum == null)
                                {
                                    trigger.FireValueConditionMetOnPreviousCall = false;  // this covers the case when the current datum is null. for some probes, the null datum is meaningful and is emitted in order for their state to be tracked appropriately (e.g., POI probe).
                                    return;
                                }
                            }

                            // get the value that might trigger the script -- it might be null in the case where the property is nullable and is not set (e.g., facebook fields, input locations, etc.)
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
                                                              .Where(input => !string.IsNullOrWhiteSpace(input.DefinedVariable))
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
            await UnscheduleCallbacksAsync();
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
            RunTimes.Clear();
            CompletionTimes.Clear();
        }

        public async Task RestartAsync()
        {
            await StopAsync();
            await StartAsync();
        }

        public async Task StopAsync()
        {
            await UnscheduleCallbacksAsync();
            await SensusServiceHelper.Get().RemoveScriptsForRunnerAsync(this, true);
        }

        public async Task ScheduleScriptRunsAsync()
        {
            if (_scheduleTrigger.WindowCount == 0 ||               // there are no windows to schedule
                SensusServiceHelper.Get() == null ||               // the service helper hasn't loaded
                Probe == null ||                                   // there is no probe
                Probe.Protocol.State == ProtocolState.Stopped ||   // protocol has stopped
                Probe.Protocol.State == ProtocolState.Stopping ||  // protocol is about to stop
                !_enabled)                                         // script is disabled
            {
                return;
            }

            // remove old callbacks and get those that need to be rescheduled
            List<ScriptTriggerTime> callbackTimesToReschedule = new List<ScriptTriggerTime>();
            lock (_scheduledCallbackTimes)
            {
                // remove any callbacks in the past
                _scheduledCallbackTimes.RemoveAll(scriptRunCallback => scriptRunCallback.Item1.NextExecution.GetValueOrDefault(DateTime.MinValue) < DateTime.Now);

                // get future callbacks that need to be rescheduled. it can happen that the app crashes or is killed 
                // and then resumes, and it's important for the times of the scheduled callbacks to be 
                // maintained. this is particularly important for runs that are very far out in the future. 
                // if the app is restarted more often than the script is run, then the script will likely never be fired.
                foreach (Tuple<ScheduledCallback, ScriptTriggerTime> callbackTime in _scheduledCallbackTimes)
                {
                    if (!SensusContext.Current.CallbackScheduler.ContainsCallback(callbackTime.Item1))
                    {
                        callbackTimesToReschedule.Add(callbackTime.Item2);
                    }
                }
            }

            // reschedule as needed
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

            // get all trigger times starting from today
            foreach (ScriptTriggerTime triggerTime in _scheduleTrigger.GetTriggerTimes(DateTime.Now, _maxAge))
            {
                // schedule all future runs, except those beyond the protocol's end date (if there is one)
                if (triggerTime.Trigger > DateTime.Now && (Probe.Protocol.ContinueIndefinitely || triggerTime.Trigger < Probe.Protocol.EndDate))
                {
                    // ensure we have sufficient background time on ios. the current method is called both when servicing a callback
                    // as well as when restarting the app upon receipt of a push notification. in both cases, we need to be sensitive
                    // to the alotted background execution time remaining. the scheduling operating can be slow on ios because it 
                    // can involve the submission of a push notification request to the remote data store. we don't want to run afoul
                    // of background execution time constraints as a result.
#if __IOS__
                    if (UIKit.UIApplication.SharedApplication.BackgroundTimeRemaining < 10)
                    {
                        SensusServiceHelper.Get().Logger.Log("Running out of background time. Aborting scheduling script runs.", LoggingLevel.Normal, GetType());
                        break;
                    }
#endif

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
        }

        private async Task ScheduleScriptRunAsync(ScriptTriggerTime triggerTime)
        {
            ScheduledCallback callback = CreateScriptRunCallback(triggerTime);

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

        private ScheduledCallback CreateScriptRunCallback(ScriptTriggerTime triggerTime)
        {
            Script scriptToRun = Script.Copy(true);
            scriptToRun.ExpirationDate = triggerTime.Expiration;
            scriptToRun.ScheduledRunTime = triggerTime.Trigger;

            ScheduledCallback callback = new ScheduledCallback(async (callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                SensusServiceHelper.Get().Logger.Log($"Running script on callback ({callbackId})", LoggingLevel.Normal, GetType());

                if (!Probe.Running || !_enabled)
                {
                    return;
                }

                await RunAsync(scriptToRun);

                lock (_scheduledCallbackTimes)
                {
                    _scheduledCallbackTimes.RemoveAll(scriptRunCallbackTime => scriptRunCallbackTime.Item1.Id == callbackId);
                }

                // on android, the callback alarm has fired and the script has been run. on ios, the notification has been
                // delivered (1) to the app in the foreground, (2) to the notification tray where the user has opened
                // it, or (3) via push notification in the background. in any case, the script has been run. now is a good 
                // time to update the scheduled callbacks to run this script.
                await ScheduleScriptRunsAsync();

            }, triggerTime.TimeTillTrigger, GetType().FullName + "-" + (triggerTime.Trigger - DateTime.MinValue).Days + "-" + triggerTime.Window, Script.Id, Probe.Protocol);  // use Script.Id rather than script.Id for the callback domain. using the former means that callbacks are unique to the script runner and not the script copies (the latter) that we will be running. the latter would always be unique.

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

            callback.DisplayPage = DisplayPage.PendingSurveys;
#endif

            return callback;
        }

        private async Task UnscheduleCallbacksAsync()
        {
            List<ScheduledCallback> scriptRunCallbacksToUnschedule = new List<ScheduledCallback>();

            lock (_scheduledCallbackTimes)
            {
                if (_scheduledCallbackTimes.Count == 0 || SensusServiceHelper.Get() == null)
                {
                    return;
                }

                scriptRunCallbacksToUnschedule = _scheduledCallbackTimes.Select(t => t.Item1).ToList();

                _scheduledCallbackTimes.Clear();
            }

            foreach (ScheduledCallback callback in scriptRunCallbacksToUnschedule)
            {
                await SensusContext.Current.CallbackScheduler.UnscheduleCallbackAsync(callback);
            }
        }

        private async Task RunAsync(Script script, Datum previousDatum = null, Datum currentDatum = null)
        {
            SensusServiceHelper.Get().Logger.Log($"Running \"{Name}\".", LoggingLevel.Normal, GetType());

            script.RunTime = DateTimeOffset.UtcNow;

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
                Tuple<bool, DateTimeOffset?> deliverFutureTime = await Probe.Agent.DeliverSurveyNow(script);

                if (!deliverFutureTime.Item1)
                {
                    if (deliverFutureTime.Item2 == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Cancelling survey at agent's request.", LoggingLevel.Normal, GetType());
                    }
                    else if (deliverFutureTime.Item2.Value > DateTimeOffset.UtcNow)
                    {
                        SensusServiceHelper.Get().Logger.Log("Rescheduling survey for " + deliverFutureTime.Item2.Value, LoggingLevel.Normal, GetType());

                        // check whether we need to expire the rescheduled script at some future point
                        DateTime? expiration = null;
                        DateTime trigger = deliverFutureTime.Item2.Value.LocalDateTime;
                        if (_maxAge.HasValue)
                        {
                            expiration = trigger + _maxAge.Value;
                        }

                        // there is no window, so just add a descriptive, unique descriptor in place of the window
                        ScriptTriggerTime triggerTime = new ScriptTriggerTime(trigger, expiration, "DEFERRED-" + Guid.NewGuid());

                        // schedule the trigger
                        await ScheduleScriptRunAsync(triggerTime);
                    }
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log("Warning:  Survey reschedule time is in the past:  " + deliverFutureTime.Item2.Value, LoggingLevel.Normal, GetType());
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
            Probe.Agent?.Observe(script, ScriptState.Delivered);
            await Probe.StoreDatumAsync(new ScriptStateDatum(ScriptState.Delivered, script.RunTime.Value, script), CancellationToken.None);
        }
    }
}