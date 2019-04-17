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
using Sensus.Probes.Location;

namespace ExampleSensingAgent
{
    public class ExampleAccelerationSensingAgent : SensingAgent
    {
        private Dictionary<Type, Queue<IDatum>> _typeData;
        private TimeSpan? _actionInterval;
        private TimeSpan _observationDuration;
        private double _threshold;
        private TimeSpan _actionCompletionCheckInterval;
        private SensingAgentState _state;

        private readonly object _stateLocker = new object();

        public override string Description => "Continuous for " + _actionCompletionCheckInterval + " if ALM > " + _threshold + " for " + _observationDuration;

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
            _actionCompletionCheckInterval = TimeSpan.FromSeconds(5);
            _state = SensingAgentState.Idle;
        }

        public override Task SetPolicyAsync(JObject policy)
        {
            _actionInterval = TimeSpan.Parse(policy.GetValue("action-interval").ToString());
            _observationDuration = TimeSpan.Parse(policy.GetValue("observation-duration").ToString());
            _threshold = double.Parse(policy.GetValue("threshold").ToString());
            _actionCompletionCheckInterval = TimeSpan.Parse(policy.GetValue("action-completion-check-interval").ToString());

            return Task.CompletedTask;
        }

        public override async Task<ActionCompletionCheck> ObserveAsync(IDatum datum)
        {
            // accumulate data by type for later analysis
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
            }

            ActionCompletionCheck actionCompletionCheck = null;

            // example of how to initiate a sensing control action in response to observed data
            if (datum is IProximityDatum)
            {
                IProximityDatum proximityDatum = datum as IProximityDatum;

                // check whether the device is near a surface (e.g., face)
                if (proximityDatum.Distance < proximityDatum.MaxDistance)
                {
                    bool initiateAction = false;

                    lock (_stateLocker)
                    {
                        // only initiate action if we're currently idle, so we don't stomp on
                        // the action interval loop's observation-action sequence. essentially
                        // we'll only be idle here if incoming data are opportunistically rather
                        // than directly observed.
                        if (_state == SensingAgentState.Idle)
                        {
                            _state = SensingAgentState.ActionOngoing;
                            initiateAction = true;
                        }
                    }

                    if (initiateAction)
                    {
                        actionCompletionCheck = await InitiateSensingControlAsync();
                    }
                }
            }

            return actionCompletionCheck;
        }

        public override async Task<ActionCompletionCheck> ActAsync(CancellationToken cancellationToken)
        {
            lock (_stateLocker)
            {
                // only observe if we're currently idle
                if (_state == SensingAgentState.Idle)
                {
                    _state = SensingAgentState.Observing;
                }
                else
                {
                    return null;
                }
            }

            // clear data queues for a fresh observation
            lock (_typeData)
            {
                _typeData.Clear();
            }

            // observe data for a window of time. the current method is run as a scheduled callback, so we're guaranteed
            // to have some amount of background time. watch out for background time expiration on iOS by monitoring the
            // passed cancellation token.
            await ObserveAsync(_observationDuration, cancellationToken);

            ActionCompletionCheck actionCompletionCheck = null;

            if (GetAbsoluteDeviationOfAverageLinearMagnitude().GetValueOrDefault() >= _threshold)
            {
                actionCompletionCheck = await InitiateSensingControlAsync();
            }
            else
            {
                _state = SensingAgentState.Idle;
            }

            return actionCompletionCheck;
        }

        private async Task<ActionCompletionCheck> InitiateSensingControlAsync()
        {
            _state = SensingAgentState.ActionOngoing;

            await SensusServiceHelper.KeepDeviceAwakeAsync();

            return new ActionCompletionCheck(async actionCompletionCheckCancellationToken =>
            {
                // the current check is called when a protocol is shutting down, in which case we should complete
                // the action and return to idle. in addition, the current check is called periodically while the
                // protocol remains running. if the action criterion value has fallen below the threshold, then
                // complete the action and return to idle. if neither of these is the case (i.e., the protocol is 
                // running and the action criterion remains at or above the threshold), then maintain the current
                // state and do nothing.
                if (Protocol.State != ProtocolState.Running || GetAbsoluteDeviationOfAverageLinearMagnitude() < _threshold)
                {
                    await SensusServiceHelper.LetDeviceSleepAsync();
                    _state = SensingAgentState.Idle;
                }

                return _state;

            }, _actionCompletionCheckInterval, "Sensus would like to check on a sensing task. Please open this notification", "Check complete. Thanks!");
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
