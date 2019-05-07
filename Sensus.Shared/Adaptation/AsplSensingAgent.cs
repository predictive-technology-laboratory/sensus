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

namespace Sensus.Adaptation
{
    /// <summary>
    /// A general-purpose ASPL agent. See the [adaptive sensing](xref:adaptive_sensing) article for more information.
    /// </summary>
    public class AsplSensingAgent : SensingAgent
    {
        /// <summary>
        /// <see cref="ProtocolSetting"/>s to apply when active observation begins.
        /// </summary>
        /// <value>The begin active observation settings.</value>
        public List<ProtocolSetting> BeginActiveObservationSettings { get; set; }

        /// <summary>
        /// <see cref="ProtocolSetting"/>s to apply when active observation ends.
        /// </summary>
        /// <value>The end active observation settings.</value>
        public List<ProtocolSetting> EndActiveObservationSettings { get; set; }

        /// <summary>
        /// The <see cref="AsplStatement"/>s to be checked against objective <see cref="IDatum"/> readings to
        /// determine whether sensing control is warranted.
        /// </summary>
        /// <value>The statements.</value>
        public List<AsplStatement> Statements { get; set; }

        private AsplStatement _statementToBeginControl;
        private AsplStatement _ongoingControlStatement;

        public AsplSensingAgent()
            : base("ASPL", "ASPL-Defined Agent", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
            Statements = new List<AsplStatement>();
        }

        public override async Task SetPolicyAsync(JObject policy)
        {
            BeginActiveObservationSettings = (policy["begin-active-observation-settings"] as JArray).Select(setting => setting.ToObject<ProtocolSetting>()).ToList();
            EndActiveObservationSettings = (policy["end-active-observation-settings"] as JArray).Select(setting => setting.ToObject<ProtocolSetting>()).ToList();
            Statements = (policy["statements"] as JArray).Select(statement => statement.ToObject<AsplStatement>()).ToList();

            await base.SetPolicyAsync(policy);
        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData)
        {
            bool satisfied = false;

            // if there is no ongoing control statement, then check all available statements.
            if (_ongoingControlStatement == null)
            {
                foreach (AsplStatement statement in Statements)
                {
                    if (statement.Criterion.SatisfiedBy(typeData))
                    {
                        _statementToBeginControl = statement;
                        satisfied = true;
                        break;
                    }
                }
            }
            // otherwise, recheck the criterion of the ongoing control statement. we'll continue
            // with control as long as the ongoing criterion continues to be satisfied. we will
            // not switch to a new control statement until the current one becomes unsatisfied.
            else
            {
                satisfied = _ongoingControlStatement.Criterion.SatisfiedBy(typeData);
            }

            return satisfied;
        }

        protected override async Task OnStateChangedAsync(SensingAgentState previousState, SensingAgentState currentState, CancellationToken cancellationToken)
        {
            await base.OnStateChangedAsync(previousState, currentState, cancellationToken);

            if (currentState == SensingAgentState.ActiveObservation && BeginActiveObservationSettings != null)
            {
                await (Protocol as Protocol).ApplySettingsAsync(BeginActiveObservationSettings, cancellationToken);
            }

            if (previousState == SensingAgentState.ActiveObservation && EndActiveObservationSettings != null)
            {
                await (Protocol as Protocol).ApplySettingsAsync(EndActiveObservationSettings, cancellationToken);
            }

            if (currentState == SensingAgentState.OpportunisticControl || currentState == SensingAgentState.ActiveControl)
            {
                // hang on to the newly satisified statement. we need ensure that the 
                // same statement used to begin control is also used to end it.
                _ongoingControlStatement = _statementToBeginControl;

                // reset the newly satisfied statement. we won't set it again until 
                // control has ended and we've reset the ongoing control statement.
                _statementToBeginControl = null;

                SensusServiceHelper.Logger.Log("Applying start-control settings for statement:  " + _ongoingControlStatement.Id, LoggingLevel.Normal, GetType());
                await (Protocol as Protocol).ApplySettingsAsync(_ongoingControlStatement.BeginControlSettings, cancellationToken);

                // update state description to include the ongoing control statement
                StateDescription += ":  " + _ongoingControlStatement.Id;
            }

            if (currentState == SensingAgentState.EndingControl)
            {
                SensusServiceHelper.Logger.Log("Applying end-control settings for statement:  " + _ongoingControlStatement.Id, LoggingLevel.Normal, GetType());
                await (Protocol as Protocol).ApplySettingsAsync(_ongoingControlStatement.EndControlSettings, cancellationToken);
                _ongoingControlStatement = null;
            }
        }
    }
}