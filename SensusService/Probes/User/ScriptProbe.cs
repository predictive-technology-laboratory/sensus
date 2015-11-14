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

namespace SensusService.Probes.User
{
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

        protected override float RawParticipation
        {
            get
            {
                int scriptsRun = _scriptRunners.Sum(scriptRunner => scriptRunner.RunTimes.Count(runTime => runTime >= Protocol.ParticipationHorizon));
                int scriptsCompleted = _scriptRunners.Sum(scriptRunner => scriptRunner.CompletionTimes.Count(completionTime => completionTime >= Protocol.ParticipationHorizon));
                return scriptsRun == 0 ? 1 : scriptsCompleted / (float)scriptsRun;
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
                scriptRunner.Initialize();
        }

        public override void Start()
        {
            base.Start();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                scriptRunner.Start();            
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                foreach (ScriptRunner scriptRunner in _scriptRunners)
                    if (scriptRunner.TestHealth(ref error, ref warning, ref misc))
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

        public override void ResetForSharing()
        {
            base.ResetForSharing();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                scriptRunner.ClearForSharing();
        }

        public override void Stop()
        {
            base.Stop();

            foreach (ScriptRunner scriptRunner in _scriptRunners)
                scriptRunner.Stop();
        }
    }
}