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

using System.Threading.Tasks;
using Estimote.Android.Indoor;

namespace Sensus.Android.Probes.Location
{
    public class AndroidEstimoteIndoorCloudCallback : Java.Lang.Object, ICloudCallback
    {
        private TaskCompletionSource<Estimote.Android.Indoor.Location> _valueSource;

        public AndroidEstimoteIndoorCloudCallback()
        {
            _valueSource = new TaskCompletionSource<Estimote.Android.Indoor.Location>();
        }

        public void Failure(EstimoteCloudException exception)
        {
            _valueSource.TrySetException(new System.Exception(exception.Body));
        }

        public void Success(Java.Lang.Object location)
        {
            _valueSource.TrySetResult(location as Estimote.Android.Indoor.Location);
        }

        public Task<Estimote.Android.Indoor.Location> GetValueAsync()
        {
            return _valueSource.Task;
        }
    }
}