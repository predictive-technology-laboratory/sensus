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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Syncfusion.SfChart.XForms;

namespace SensusService.Probes.User.Scripts
{
    public class ScriptProbe : Probe
    {
        private ObservableCollection<ScriptRunner> _scriptRunners;
        private int _scriptCallbacksScheduled;

        public ObservableCollection<ScriptRunner> ScriptRunners
        {
            get { return _scriptRunners; }
        }

        public int ScriptCallbacksScheduled
        {
            get { return _scriptCallbacksScheduled; }
            set { _scriptCallbacksScheduled = value; }
        }

        public sealed override string DisplayName
        {
            get { return "Scripted Interactions"; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(ScriptDatum); }
        }

        protected override float RawParticipation
        {
            get
            {
                int scriptsRun = _scriptRunners.Sum(scriptRunner => scriptRunner.RunTimes.Count(runTime => runTime >= Protocol.ParticipationHorizon));
                int scriptsCompleted = _scriptRunners.Sum(scriptRunner => scriptRunner.CompletionTimes.Count(completionTime => completionTime >= Protocol.ParticipationHorizon));
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
                    if (scriptRunner.Enabled)
                    {
                        foreach (Trigger trigger in scriptRunner.Triggers)
                            collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + scriptRunner.Name + ":  When " + trigger.Probe.DisplayName + " is " + uppercaseSplitter.Replace(trigger.Condition.ToString(), " ").ToLower() + " " + trigger.ConditionValue + ".");

                        if (scriptRunner.RunOnStart)
                            collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + scriptRunner.Name + ":  Once when the study is started.");

                        if (scriptRunner.TriggerWindows != "")
                        {
                            string windows = scriptRunner.TriggerWindows;
                            string collectionDescriptionPrefix = "Randomly during hours ";
                            int commaCount = windows.Count(c => c == ',');
                            if (commaCount == 0)
                            {
                                if (!windows.Contains('-'))
                                    collectionDescriptionPrefix = "At ";
                            }
                            else if (commaCount == 1)
                                windows = windows.Replace(",", " and");
                            else if (commaCount > 1)
                                windows = windows.Insert(windows.LastIndexOf(',') + 1, " and");

                            collectionDescription.Append((collectionDescription.Length == 0 ? "" : Environment.NewLine) + scriptRunner.Name + ":  " + collectionDescriptionPrefix + windows + ".");
                        }
                    }

                return collectionDescription.ToString();
            }
        }

        public ScriptProbe()
        {            
            _scriptRunners = new ObservableCollection<ScriptRunner>();
            _scriptCallbacksScheduled = 0;
        }

        protected override void Initialize()
        {
            base.Initialize();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                if (scriptRunner.Enabled)
                    scriptRunner.Initialize();
        }

        protected override void InternalStart()
        {
            base.InternalStart();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                if (scriptRunner.Enabled)
                    scriptRunner.Start();            
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                foreach (ScriptRunner scriptRunner in _scriptRunners)
                    if (scriptRunner.Enabled && scriptRunner.TestHealth(ref error, ref warning, ref misc))
                    {
                        warning += "Restarting script runner \"" + scriptRunner.Name + "\"." + Environment.NewLine;

                        try
                        {
                            scriptRunner.Restart();
                        }
                        catch (Exception ex)
                        {
                            warning += "Error restarting script runner \"" + scriptRunner.Name + "\":  " + ex.Message;
                            SensusServiceHelper.Get().Logger.Log(warning, LoggingLevel.Normal, GetType());
                        }
                    }
            }

            return restart;
        }

        public override void Reset()
        {
            base.Reset();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                scriptRunner.Reset();

            _scriptCallbacksScheduled = 0;
        }

        public override void Stop()
        {
            base.Stop();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                scriptRunner.Stop();

            _scriptCallbacksScheduled = 0;
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