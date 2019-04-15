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

        public override string Description => "Acceleration Sensing Agent";

        public override string Id => "Acceleration";

        public override TimeSpan? ActionInterval => TimeSpan.FromSeconds(30);

        public override TimeSpan? ActionIntervalToleranceBefore => null;

        public override TimeSpan? ActionIntervalToleranceAfter => null;

        public ExampleAccelerationSensingAgent()
        {
            _typeData = new Dictionary<Type, Queue<IDatum>>();
        }

        public override Task SetPolicyAsync(JObject policy)
        {
            throw new NotImplementedException();
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
            List<IAccelerometerDatum> accelerometerData;

            lock (_typeData)
            {
                if (_typeData.TryGetValue(typeof(IAccelerometerDatum), out Queue<IDatum> data))
                {
                    accelerometerData = data.Cast<IAccelerometerDatum>().ToList();
                }
                else
                {
                    return null;
                }
            }

            double averageLinearMagnitude = accelerometerData.Average(accelerometerDatum => Math.Sqrt(Math.Pow(accelerometerDatum.X, 2) + Math.Pow(accelerometerDatum.Y, 2) + Math.Pow(accelerometerDatum.Z, 2)));

            CompletionAction completionAction = null;

            if (averageLinearMagnitude > 1)
            {
                await SensusServiceHelper.KeepDeviceAwakeAsync();

                completionAction = new CompletionAction(async completionActionCancellationToken =>
                {
                    await SensusServiceHelper.LetDeviceSleepAsync();

                }, TimeSpan.FromSeconds(30));
            }

            return completionAction;
        }
    }
}
