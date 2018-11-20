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

namespace Sensus
{
    /// <summary>
    /// Provides information on the alive status of the app. Will be submitted to the <see cref="DataStores.Remote.RemoteDataStore"/> on an interval specified
    /// by <see cref="SensusServiceHelper.HEALTH_TEST_DELAY"/>. The <see cref="DataStores.Remote.RemoteDataStore"/> should contain a sequence of heartbeats
    /// with <see cref="Datum.Timestamp"/> values according to this interval. For example, if the interval is 6 hours, then there should be a sequence of
    /// 4 <see cref="HeartbeatDatum"/> readings in the <see cref="DataStores.Remote.RemoteDataStore"/> each day.
    /// </summary>
    public class HeartbeatDatum : Datum
    {
        public override string DisplayDetail => "Heartbeat";

        public override object StringPlaceholderValue => "Heartbeat";

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private HeartbeatDatum()
        {
        }

        public HeartbeatDatum(DateTimeOffset timestamp)
            : base(timestamp)
        {
        }
    }
}