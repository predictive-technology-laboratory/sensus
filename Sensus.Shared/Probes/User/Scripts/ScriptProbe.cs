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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Syncfusion.SfChart.XForms;
using System.Collections.Generic;
using Microsoft.AppCenter.Analytics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// The Script Probe allows Sensus to deliver custom surveys to the user in response to either a schedule or the data coming off other probes. For 
    /// example, one could configure a survey to display at particular times or randomly within particular time blocks. One could also configure a survey 
    /// to display when a <see cref="Datum"/> from another probe meets particular criteria.
    /// </summary>
    public class ScriptProbe : Probe
    {
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
                // attempt to lazy-load the agent if there is none
                if (_agent == null)
                {
                    try
                    {
#if __ANDROID__
                        _agent = GetAgent(AgentAssemblyBytes, AgentId);
#elif __IOS__
                        _agent = GetAgent(AgentId);
#endif
                    }
                    catch (Exception)
                    {
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

            Agent?.Reset();
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

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                // ensure that surveys are scheduled to date
                await scriptRunner.ScheduleScriptRunsAsync();

                string eventName = TrackedEvent.Health + ":" + GetType().Name;
                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "Triggers Scheduled", scriptRunner.ScriptRunCallbacks.Count.ToString() }
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