//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
