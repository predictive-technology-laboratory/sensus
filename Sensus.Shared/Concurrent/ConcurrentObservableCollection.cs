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

using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Sensus.Shared.Context;

namespace Sensus.Shared.Concurrent
{
    /// <summary>
    /// This is terrifying and a bad idea on many levels. However, I think it is still better than sprinkling lock code throughout the entire system.
    /// Someday Microsoft might come up with a clever lock free way to design random access data structures. When that happens we'll get rid of this.
    /// </summary>
    public class ConcurrentObservableCollection<T>: INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>
    {
        private readonly IConcurrent _concurrent;

        #region Fields
        private readonly ObservableCollection<T> _observableCollection;
        #endregion

        #region Events
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _observableCollection.CollectionChanged += value; }
            remove { _observableCollection.CollectionChanged -= value; }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { (_observableCollection as INotifyPropertyChanged).PropertyChanged += value; }
            remove { (_observableCollection as INotifyPropertyChanged).PropertyChanged -= value; }
        }
        #endregion

        #region Properties
        public int Count => _observableCollection.Count;

        bool ICollection<T>.IsReadOnly => (_observableCollection as ICollection<T>).IsReadOnly;
        #endregion

        #region Constructors
        public ConcurrentObservableCollection()
        {
            _concurrent = SensusContext.Current.MainThreadSynchronizer;
            _observableCollection = new ObservableCollection<T>();
        }

        public ConcurrentObservableCollection(IConcurrent concurrent)
        {
            _concurrent           = concurrent;
            _observableCollection = new ObservableCollection<T>();
        }
        #endregion

        #region Public Methods
        public IEnumerator<T> GetEnumerator()
        {
            return _concurrent.ExecuteThreadSafe(() => Materialize().GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _concurrent.ExecuteThreadSafe(() => _observableCollection.Add(item));
        }

        public void Clear()
        {
            _concurrent.ExecuteThreadSafe(() =>_observableCollection.Clear());
        }

        /// <remarks>
        /// I don't think we need to lock this action down. Unit Tests seem to confirm this idea.
        /// </remarks>
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public bool Contains(T item)
        {
            return _observableCollection.Contains(item);
        }

        /// <remarks>
        /// I don't think we need to lock this action down. Unit Tests seem to confirm this idea.
        /// </remarks>
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void CopyTo(T[] array, int arrayIndex)
        {
            _observableCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _concurrent.ExecuteThreadSafe(() => _observableCollection.Remove(item));
        }

        public void Insert(int index, T item)
        {
            _concurrent.ExecuteThreadSafe(() => _observableCollection.Insert(index, item)) ;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// The enumerator returned by ObservableCollection uses deferred execution.
        /// This deferred execution leaves us open to Concurrency Bugs if it runs while another thread updates.
        /// Therefore, we need to iterate our enumerator and materialize it while we still have a lock.
        /// Interestingly, we are still able to use deferred execution as long as we materialize all at once.
        /// </summary>
        private List<T> Materialize()
        {
            var output = new List<T>();

            using (var enumerator = _observableCollection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    output.Add(enumerator.Current);
                }
            }

            return output;
        }
        #endregion
    }
}   
