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
        private TimeSpan _completionActionInterval;
        private State _state;

        private readonly object _stateLocker = new object();

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
            _state = State.Idle;
        }

        public override Task SetPolicyAsync(JObject policy)
        {
            _actionInterval = TimeSpan.Parse(policy.GetValue("action-interval").ToString());
            _observationDuration = TimeSpan.Parse(policy.GetValue("observation-duration").ToString());
            _threshold = double.Parse(policy.GetValue("threshold").ToString());
            _completionActionInterval = TimeSpan.Parse(policy.GetValue("completion-action-interval").ToString());

            return Task.CompletedTask;
        }

        public override async Task<CompletionAction> ObserveAsync(IDatum datum)
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
            }

            if (datum is IProximityDatum)
            {
                IProximityDatum proximityDatum = datum as IProximityDatum;

                if (proximityDatum.Distance < proximityDatum.MaxDistance)
                {
                    lock (_stateLocker)
                    {
                        if (_state == State.Idle)
                        {
                            _state = State.ActionOngoing;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return await TransitionToOngoingAsync();
                }
            }

            return null;
        }

        public override async Task<CompletionAction> ActAsync(CancellationToken cancellationToken)
        {
            lock (_stateLocker)
            {
                if (_state == State.Idle)
                {
                    _state = State.Observing;
                }
                else
                {
                    return null;
                }
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

            if (GetAbsoluteDeviationOfAverageLinearMagnitude().GetValueOrDefault() > _threshold)
            {
                return await TransitionToOngoingAsync();
            }
            else
            {
                _state = State.Idle;
                return null;
            }
        }

        private async Task<CompletionAction> TransitionToOngoingAsync()
        {
            _state = State.ActionOngoing;

            await SensusServiceHelper.KeepDeviceAwakeAsync();

            return new CompletionAction(async completionActionCancellationToken =>
            {
                if (Protocol.State != ProtocolState.Running || GetAbsoluteDeviationOfAverageLinearMagnitude() < _threshold)
                {
                    await SensusServiceHelper.LetDeviceSleepAsync();
                    _state = State.Idle;
                }

                return _state;

            }, _completionActionInterval);
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
