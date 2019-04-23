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

namespace Sensus.AdaptiveSensing
{
    public class AsplSensingAgent : SensingAgent
    {
        public AsplSensingAgent()
            : base("ASPL", "ASPL-Defined Agent", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        {
        }

        public override Task SetPolicyAsync(JObject policy)
        {
            return base.SetPolicyAsync(policy);


        }

        protected override bool ObservedDataMeetControlCriterion(Dictionary<Type, List<IDatum>> typeData, IDatum opportunisticDatum)
        {
            throw new NotImplementedException();
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

        protected override void UpdateObservedData(Dictionary<Type, List<IDatum>> typeData)
        {
            throw new NotImplementedException();
        }
    }
}
