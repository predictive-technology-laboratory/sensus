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

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// The Script Probe allows Sensus to deliver custom surveys to the user in response to either a schedule or the data coming off other probes. For 
    /// example, one could configure a survey to display at particular times or randomly within particular time blocks. One could also configure a survey 
    /// to display when a <see cref="Datum"/> from another probe meets particular criteria.
    /// </summary>
    public class ScriptProbe : Probe
    {
        private ObservableCollection<ScriptRunner> _scriptRunners;

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

        protected override void Initialize()
        {
            base.Initialize();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                if (scriptRunner.Enabled)
                {
                    scriptRunner.Initialize();
                }
            }
        }

        protected override void ProtectedStart()
        {
            base.ProtectedStart();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                if (scriptRunner.Enabled)
                {
                    scriptRunner.Start();
                }
            }
        }

        public override bool TestHealth(ref List<Tuple<string, Dictionary<string, string>>> events)
        {
            bool restart = base.TestHealth(ref events);

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                // ensure that surveys are scheduled
                scriptRunner.ScheduleScriptRuns();

                string eventName = TrackedEvent.Health + ":" + GetType().Name;
                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "Triggers Scheduled", scriptRunner.ScriptRunCallbacks.Count.ToString() }
                };

                Analytics.TrackEvent(eventName, properties);

                events.Add(new Tuple<string, Dictionary<string, string>>(eventName, properties));
            }

            return restart;
        }

        public override void Reset()
        {
            base.Reset();

            foreach (var scriptRunner in _scriptRunners)
            {
                scriptRunner.Reset();
            }
        }

        public override void Stop()
        {
            base.Stop();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
            {
                scriptRunner.Stop();
            }
        }

        protected override ChartSeries GetChartSeries()
        {
            return null;
        }

        protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
        {
            return null;
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