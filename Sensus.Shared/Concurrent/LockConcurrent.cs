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
using Sensus.Dispose;

namespace Sensus.Concurrent
{
    public class LockConcurrent: Disposable, IConcurrent
    {
        #region Fields
        private readonly object _lock;
        #endregion

        #region Constructors
        public LockConcurrent(object @lock = null)
        {
            _lock = @lock ?? this;
        }
        #endregion

        #region Public Methods
        public void ExecuteThreadSafe(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (_lock)
            {
                action();
            }
        }

        public T ExecuteThreadSafe<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            lock (_lock)
            {
                return func();
            }
        }

        #endregion
    }
}