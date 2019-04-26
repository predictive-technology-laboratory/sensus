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
    /// <summary>
    /// A general-purpose ASPL agent. See the [adaptive sensing](xref:adaptive_sensing) article for more information.
    /// </summary>
    public class AsplSensingAgent : SensingAgent
    {
        /// <summary>
        /// The <see cref="AsplStatement"/>s to be checked against objective <see cref="IDatum"/> readings to
        /// determine whether sensing control is warranted.
        /// </summary>
        /// <value>The statements.</value>
        public List<AsplStatement> Statements { get; set; }

        private AsplStatement _currentlySatisfiedStatement;
        private AsplStatement _ongoingControlStatement;

        private readonly object _statementLocker = new object();

        public AsplSensingAgent()
            : base("ASPL", "ASPL-Defined Agent", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
            Statements = new List<AsplStatement>();
        }

        public override async Task SetPolicyAsync(JObject policy)
        {
            Statements = (policy["statements"] as JArray).Select(statement => statement.ToObject<AsplStatement>()).ToList();

            await base.SetPolicyAsync(policy);
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            lock (_statementLocker)
            {
                bool satisfied = false;

                foreach (AsplStatement statement in Statements)
                {
                    if (statement.Criterion.SatisfiedBy(typeData))
                    {
                        _currentlySatisfiedStatement = statement;
                        satisfied = true;
                        break;
                    }
                }

                return satisfied;
            }
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
            lock (_statementLocker)
            {
                if (_ongoingControlStatement == null && _currentlySatisfiedStatement != null)
                {
                    // hang on to the currently satisified statement. we need ensure that the 
                    // same statement used to begin control is also used to end it.
                    _ongoingControlStatement = _currentlySatisfiedStatement;
                }
                else
                {
                    return;
                }
            }

            SensusServiceHelper.Logger.Log("Applying start-control settings for statement:  " + _ongoingControlStatement.Id, LoggingLevel.Normal, GetType());
            await (Protocol as Protocol).ApplySettingsAsync(_ongoingControlStatement.BeginControlSettings, cancellationToken);
        }

        protected override async Task OnEndingControlAsync(CancellationToken cancellationToken)
        {
            AsplStatement ongoingControlStatement;

            lock (_statementLocker)
            {
                if (_ongoingControlStatement == null)
                {
                    return;
                }
                else
                {
                    ongoingControlStatement = _ongoingControlStatement;
                    _ongoingControlStatement = null;
                }
            }

            SensusServiceHelper.Logger.Log("Applying end-control settings for statement:  " + ongoingControlStatement.Id, LoggingLevel.Normal, GetType());
            await (Protocol as Protocol).ApplySettingsAsync(ongoingControlStatement.EndControlSettings, cancellationToken);
        }
    }
}