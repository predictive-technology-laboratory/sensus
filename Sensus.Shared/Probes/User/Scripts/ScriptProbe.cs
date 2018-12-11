//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using Microsoft.AppCenter.Analytics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;
using Sensus.Callbacks;
using Sensus.Exceptions;

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// The Script Probe allows Sensus to deliver custom surveys to the user in response to either a schedule or the data coming off other probes. For 
    /// example, one could configure a survey to display at particular times or randomly within particular time blocks. One could also configure a survey 
    /// to display when a <see cref="Datum"/> from another probe meets particular criteria.
    /// </summary>
    public class ScriptProbe : Probe
    {

        // android allows us to dynamically load code assemblies, but iOS does not. so, the current approach
        // is to only support dynamic loading on android and force compile-time assembly inclusion on ios.
#if __ANDROID__

        public static IScriptProbeAgent GetAgent(byte[] assemblyBytes, string agentId)
        {
            return GetAgents(assemblyBytes).SingleOrDefault(agent => agent.Id == agentId);
        }

        public static List<IScriptProbeAgent> GetAgents(byte[] assemblyBytes)
        {
            return Assembly.Load(assemblyBytes)
                           .GetTypes()
                           .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(IScriptProbeAgent)))
                           .Select(Activator.CreateInstance)
                           .Cast<IScriptProbeAgent>()
                           .ToList();
        }

        /// <summary>
        /// Bytes of the assembly in which the <see cref="Agent"/> is contained.
        /// </summary>
        /// <value>The agent assembly bytes.</value>
        public byte[] AgentAssemblyBytes { get; set; }

#elif __IOS__

        public static IScriptProbeAgent GetAgent(string agentId)
        {
            return GetAgents().SingleOrDefault(agent => agent.Id == agentId);
        }

        public static List<IScriptProbeAgent> GetAgents()
        {
            // get agents from the current assembly. they must be linked at compile time.
            return Assembly.GetAssembly(typeof(ExampleScriptProbeAgent.ExampleRandomScriptProbeAgent))
                           .GetTypes()
                           .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(IScriptProbeAgent)))
                           .Select(Activator.CreateInstance)
                           .Cast<IScriptProbeAgent>()
                           .ToList();
        }

#endif

        private ObservableCollection<ScriptRunner> _scriptRunners;
        private IScriptProbeAgent _agent;

        /// <summary>
        /// Gets or sets the agent that controls survey delivery. See [here](xref:adaptive_surveys) for more information.
        /// </summary>
        /// <value>The agent.</value>
        [JsonIgnore]
        public IScriptProbeAgent Agent
        {
            get
            {
                // attempt to lazy-load the agent if there is none and we an agent id
                if (_agent == null && !string.IsNullOrWhiteSpace(AgentId))
                {
                    try
                    {
#if __ANDROID__
                        // also require an assembly on android, which is where we get the agents from.
                        if (AgentAssemblyBytes != null)
                        {
                            _agent = GetAgent(AgentAssemblyBytes, AgentId);
                        }
#elif __IOS__
                        // there is no assembly in ios per apple restrictions on dynamically loaded code. agents are baked into the app instead.
                        _agent = GetAgent(AgentId);
#endif

                        // set the agent's policy if we previously received one (e.g., via push notification)
                        if (!string.IsNullOrWhiteSpace(AgentPolicyJSON))
                        {
                            _agent.SetPolicy(AgentPolicyJSON);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get()?.Logger.Log("Exception while loading agent:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                return _agent;
            }
            set
            {
                _agent = value;
                AgentId = _agent?.Id;
            }
        }

        /// <summary>
        /// Id of the <see cref="Agent"/> to use.
        /// </summary>
        /// <value>The agent identifier.</value>
        public string AgentId { get; set; }

        /// <summary>
        /// Gets or sets the agent policy JSON.
        /// </summary>
        /// <value>The agent policy JSON.</value>
        public string AgentPolicyJSON { get; set; }

        public ObservableCollection<ScriptRunner> ScriptRunners
        {
            get { return _scriptRunners; }
        }

        public sealed override string DisplayName
        {
            get { return "Scripted Interactions"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(ScriptDatum); }
        }

        protected override long DataRateSampleSize => 10;

        public override double? MaxDataStoresPerSecond { get => null; set { } }

        protected override double RawParticipation
        {
            get
            {
                int scriptsRun = _scriptRunners.Sum(scriptRunner =>
                {
                    lock (scriptRunner.RunTimes)
                    {
                        return scriptRunner.RunTimes.Count(runTime => runTime >= Protocol.ParticipationHorizon);
                    }
                });

                int scriptsCompleted = _scriptRunners.Sum(scriptRunner =>
                {
                    lock (scriptRunner.CompletionTimes)
                    {
                        return scriptRunner.CompletionTimes.Count(completionTime => completionTime >= Protocol.ParticipationHorizon);
                    }
                });

                return scriptsRun == 0 ? 1 : scriptsCompleted / (float)scriptsRun;
            }
        }

        public override string CollectionDescription
        {
            get
            {
                StringBuilder collectionDescription = new StringBuilder();

                Regex uppercaseSplitter = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                (?<=[^A-Z])(?=[A-Z]) |
                (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                foreach (ScriptRunner scriptRunner in _scriptRunners)
                {
                    if (scriptRunner.Enabled)
                    {
                        foreach (Trigger trigger in scriptRunner.Triggers)
                        {
                            collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + scriptRunner.Name + ":  When " + trigger.Probe.DisplayName + " is " + uppercaseSplitter.Replace(trigger.Condition.ToString(), " ").ToLower() + " " + trigger.ConditionValue + ".");
                        }

                        if (scriptRunner.RunOnStart)
                        {
                            collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + scriptRunner.Name + ":  Once when the study is started.");
                        }

                        if (scriptRunner.TriggerWindowsString != "")
                        {
                            collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + scriptRunner.Name + ":  " + scriptRunner.ScheduleTriggerReadableDescription + ".");
                        }
                    }
                }

                return collectionDescription.ToString();
            }
        }

        public ScriptProbe()
        {
            _scriptRunners = new ObservableCollection<ScriptRunner>();
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                if (scriptRunner.Enabled)
                {
                    await scriptRunner.InitializeAsync();
                }
            }

            Agent?.Reset(SensusServiceHelper.Get());
        }

        protected override async Task ProtectedStartAsync()
        {
            await base.ProtectedStartAsync();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                if (scriptRunner.Enabled)
                {
                    await scriptRunner.StartAsync();
                }
            }
        }

        public override async Task<HealthTestResult> TestHealthAsync(List<AnalyticsTrackedEvent> events)
        {
            HealthTestResult result = await base.TestHealthAsync(events);

            // ensure that each window-based script runner that is enabled has scheduled surveys
            foreach (ScriptRunner scriptRunner in _scriptRunners.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.TriggerWindowsString)))
            {
                // update scheduled surveys
                await scriptRunner.ScheduleScriptRunsAsync();

                // ensure that at least 1 callback is scheduled for the future
                int triggersScheduled = scriptRunner.ScriptRunCallbacks.Count(scheduledCallback => scheduledCallback.State == ScheduledCallbackState.Scheduled &&
                                                                                                   scheduledCallback.NextExecution != null &&
                                                                                                   (scheduledCallback.NextExecution.Value - DateTime.Now).Ticks > 0);

                if (triggersScheduled <= 0)
                {
                    SensusException.Report("Script runner \"" + scriptRunner.Name + "\" is enabled with a window trigger, but it has no scheduled callbacks.");
                }

                string eventName = TrackedEvent.Health + ":" + GetType().Name;
                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "Triggers Scheduled", triggersScheduled.ToString() }
                };

                Analytics.TrackEvent(eventName, properties);

                events.Add(new AnalyticsTrackedEvent(eventName, properties));
            }

            return result;
        }

        public override async Task ResetAsync()
        {
            await base.ResetAsync();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                await scriptRunner.ResetAsync();
            }
        }

        public override async Task StopAsync()
        {
            await base.StopAsync();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                await scriptRunner.StopAsync();
            }
        }

        protected override ChartSeries GetChartSeries()
        {
            throw new NotImplementedException();
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            throw new NotImplementedException();
        }

        protected override ChartAxis GetChartPrimaryAxis()
        {
            throw new NotImplementedException();
        }

        protected override RangeAxisBase GetChartSecondaryAxis()
        {
            throw new NotImplementedException();
        }
    }
}
