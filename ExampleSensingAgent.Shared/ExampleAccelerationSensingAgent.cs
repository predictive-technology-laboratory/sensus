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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sensus;
using Sensus.Probes.Movement;
using System.Linq;

namespace ExampleSensingAgent
{
    public class ExampleAccelerationSensingAgent : SensingAgent
    {
        private Dictionary<Type, Queue<IDatum>> _typeData;
        private TimeSpan? _actionInterval;
        private TimeSpan _observationDuration;
        private double _threshold;
        private TimeSpan _completionActionInterval;
        private bool _executingAction;

        public override string Description => "Continuous for " + _completionActionInterval + " if ALM > " + _threshold + " for " + _observationDuration;

        public override string Id => "Acceleration";

        public override TimeSpan? ActionInterval => _actionInterval;

        public override TimeSpan? ActionIntervalToleranceBefore => null;

        public override TimeSpan? ActionIntervalToleranceAfter => null;

        public ExampleAccelerationSensingAgent()
        {
            _typeData = new Dictionary<Type, Queue<IDatum>>();
            _actionInterval = TimeSpan.FromSeconds(10);
            _observationDuration = TimeSpan.FromSeconds(5);
            _threshold = 0.5;
            _completionActionInterval = TimeSpan.FromSeconds(30);
        }

        public override Task SetPolicyAsync(JObject policy)
        {
            _actionInterval = TimeSpan.Parse(policy.GetValue("action-interval").ToString());
            _observationDuration = TimeSpan.Parse(policy.GetValue("observation-duration").ToString());
            _threshold = double.Parse(policy.GetValue("threshold").ToString());
            _completionActionInterval = TimeSpan.Parse(policy.GetValue("completion-action-interval").ToString());

            return Task.CompletedTask;
        }

        public override Task ObserveAsync(IDatum datum)
        {
            lock (_typeData)
            {
                Type datumType = datum.GetType();

                if (!_typeData.TryGetValue(datumType, out Queue<IDatum> data))
                {
                    data = new Queue<IDatum>();
                    _typeData.Add(datumType, data);
                }

                data.Enqueue(datum);

                while (data.Count > 100)
                {
                    data.Dequeue();
                }

                return Task.CompletedTask;
            }
        }

        public override async Task<CompletionAction> ActAsync(string actionId, CancellationToken cancellationToken)
        {
            // do nothing if the agent is already executing an action
            if (_executingAction)
            {
                return null;
            }

            // clear data queues so that observation is fresh
            lock (_typeData)
            {
                _typeData.Clear();
            }

            // observe data for a window of time. the current method is run as a scheduled callback, so we're guaranteed
            // to have some amount of background time. watch out for background time expiration on iOS by monitoring the
            // passed cancellation token.
            await ObserveAsync(_observationDuration, cancellationToken);

            CompletionAction completionAction = null;

            if (GetAbsoluteDeviationOfAverageLinearMagnitude().GetValueOrDefault() > _threshold)
            {
                await SensusServiceHelper.KeepDeviceAwakeAsync();

                completionAction = new CompletionAction(async completionActionCancellationToken =>
                {
                    // if the protocol is still running, then check the criterion value against the threshold
                    // and continue sensing if a positive result is obtained.
                    if (Protocol.State == ProtocolState.Running && GetAbsoluteDeviationOfAverageLinearMagnitude() > _threshold)
                    {
                        return CompletionAction.Result.Continue;
                    }
                    // the completion action might be executing because the protocol is shutting down. if this 
                    // is the case (i.e., protocol is not running), then complete sensing and bail out.
                    else
                    {
                        await SensusServiceHelper.LetDeviceSleepAsync();
                        _executingAction = false;
                        return CompletionAction.Result.Finished;
                    }

                }, _completionActionInterval);
            }

            return completionAction;
        }

        private double? GetAbsoluteDeviationOfAverageLinearMagnitude()
        {
            List<IAccelerometerDatum> accelerometerData;

            lock (_typeData)
            {
                Type accelerometerDatumType = _typeData.Keys.SingleOrDefault(type => type.GetInterfaces().Contains(typeof(IAccelerometerDatum)));

                if (accelerometerDatumType != null && _typeData.TryGetValue(accelerometerDatumType, out Queue<IDatum> data))
                {
                    accelerometerData = data.Cast<IAccelerometerDatum>().ToList();
                }
                else
                {
                    return null;
                }
            }

            double averageLinearMagnitude = accelerometerData.Average(accelerometerDatum => Math.Sqrt(Math.Pow(accelerometerDatum.X, 2) + Math.Pow(accelerometerDatum.Y, 2) + Math.Pow(accelerometerDatum.Z, 2)));

            // acceleration values include gravity. thus, a stationary device will register 1 on one of the axes.
            // use absolute deviation from 1 as the criterion value with which to compare the threshold.
            return Math.Abs(averageLinearMagnitude - 1);
        }

        public override string ToString()
        {
            return Id + ":  " + Description;
        }
    }
}
