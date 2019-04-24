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
using Newtonsoft.Json.Linq;

namespace Sensus.AdaptiveSensing
{
    public class AsplSensingAgent : SensingAgent
    {
        private List<AsplStatement> _statements;
        private AsplStatement _satisfiedStatement;

        public AsplSensingAgent()
            : base("ASPL", "ASPL-Defined Agent", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
        }

        public override async Task SetPolicyAsync(JObject policy)
        {
            await base.SetPolicyAsync(policy);

            _statements = (policy["statements"] as JArray).Select(statement => statement.ToObject<AsplStatement>()).ToList();
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

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            foreach (AsplStatement statement in _statements)
            {
                if (statement.Criterion.SatisfiedBy(typeData))
                {
                    _satisfiedStatement = statement;
                    break;
                }
            }

            return _satisfiedStatement != null;
        }

        protected override async Task OnOpportunisticControlAsync(CancellationToken cancellationToken)
        {
            await OnControlAsync(cancellationToken);
        }

        protected override async Task OnActiveControlAsync(CancellationToken cancellationToken)
        {
            await OnControlAsync(cancellationToken);
        }

        private async Task OnControlAsync(CancellationToken cancellationToken)
        {
            await (Protocol as Protocol).ApplySettingsAsync(_satisfiedStatement.BeginControlSettings, cancellationToken);
        }

        protected override async Task OnEndingControlAsync(CancellationToken cancellationToken)
        {
            await (Protocol as Protocol).ApplySettingsAsync(_satisfiedStatement.EndControlSettings, cancellationToken);
        }
    }
}
