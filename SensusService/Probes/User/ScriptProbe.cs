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

using Newtonsoft.Json;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;

namespace SensusService.Probes.User
{
    public class ScriptProbe : Probe
    {        
        private ObservableCollection<Script> _scripts;
        private int _numScriptsAgedOut;

        private readonly object _locker = new object();

        public ObservableCollection<Script> Scripts
        {
            get { return _scripts; }
        }            

        protected override string DefaultDisplayName
        {
            get { return "Scripted Interactions"; }
        }

        public int NumScriptsAgedOut
        {
            get
            {
                return _numScriptsAgedOut;
            }
            set
            {
                _numScriptsAgedOut = value;
            }
        }

        [JsonIgnore]
        public sealed override Type DatumType
        {
            get { return typeof(ScriptDatum); }
        }

        public ScriptProbe()
        {            
            _scripts = new ObservableCollection<Script>();
            _numScriptsAgedOut = 0;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (_scripts == null)
                _scripts = new ObservableCollection<Script>();
        }

        public override void Start()
        {
            base.Start();

            foreach (Script script in _scripts)
                script.Start();            
        }                                         

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                foreach (Script script in _scripts)
                    if (script.TestHealth(ref error, ref warning, ref misc))
                    {
                        warning += "Restarting script \"" + script.Name + "\"." + Environment.NewLine;

                        try
                        {
                            script.Restart();
                        }
                        catch (Exception ex)
                        {
                            warning += "Error restarting script \"" + script.Name + "\":  " + ex.Message;
                            SensusServiceHelper.Get().Logger.Log(warning, LoggingLevel.Normal, GetType());
                        }
                    }

                if (_numScriptsAgedOut > 0)
                    misc += _numScriptsAgedOut + " scripts have aged out." + Environment.NewLine;
            }

            return restart;
        }                               

        public override void Stop()
        {
            base.Stop();

            foreach (Script script in _scripts)
                script.Stop();
        }
    }
}