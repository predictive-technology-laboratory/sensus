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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sensus.AdaptiveSensing
{
    public class AsplSensingAgent : SensingAgent
    {
        private List<AsplControlCriterion> _controlCriteria;
        private AsplControlCriteriaCombination _controlCriteriaCombination;
        private List<AsplControlAction> _beginControlActions;
        private List<AsplControlAction> _endControlActions;

        public AsplSensingAgent()
            : base("ASPL", "ASPL-Defined Agent", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
        }

        public override async Task SetPolicyAsync(JObject policy)
        {
            await base.SetPolicyAsync(policy);

            _controlCriteria = (policy["control-criteria"] as JArray).Select(controlCriterion => controlCriterion.ToObject<AsplControlCriterion>()).ToList();
            _controlCriteriaCombination = policy["control-criteria-combination"].ToObject<AsplControlCriteriaCombination>();
            _beginControlActions = (policy["begin-control-actions"] as JArray).Select(controlAction => controlAction.ToObject<AsplControlAction>()).ToList();
            _endControlActions = (policy["end-control-actions"] as JArray).Select(controlAction => controlAction.ToObject<AsplControlAction>()).ToList();
        }

        protected override void UpdateObservedData(Dictionary<Type, List<IDatum>> typeData)
        {
            foreach (Type type in typeData.Keys)
            {
                List<IDatum> data = typeData[type];

                // trim collections by size
                while (data.Count > 100)
                {
                    data.RemoveAt(0);
                }
            }
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData, IDatum opportunisticDatum)
        {
            bool criterionMet = false;

            if (opportunisticDatum == null)
            {
                List<bool> controlCriteriaSatisfied = new List<bool>();

                foreach (AsplControlCriterion controlCriterion in _controlCriteria)
                {
                    foreach (Type type in typeData.Keys)
                    {
                        controlCriteriaSatisfied.Add(controlCriterion.SatisfiedBy(type, typeData[type]));
                    }
                }

                criterionMet = _controlCriteriaCombination == AsplControlCriteriaCombination.Conjunction ? controlCriteriaSatisfied.All(satisfied => satisfied) : controlCriteriaSatisfied.Any();
            }
            else
            {

            }

            return criterionMet;
        }

        protected override Task OnActiveControlAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnEndingControlAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnOpportunisticControlAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
