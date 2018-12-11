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

using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Sensus.Context;

namespace Sensus.Concurrent
{
    /// <summary>
    /// Provides thread-safe concurrent access to an observable collection.
    /// </summary>
    public class ConcurrentObservableCollection<T>: INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>
    {
        // This is terrifying and a bad idea on many levels. However, I think it is still better than sprinkling lock code throughout the entire system.
        // Someday Microsoft might come up with a clever lock free way to design random access data structures. When that happens we'll get rid of this.

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
        public IConcurrent Concurrent
        {
            get { return _concurrent; }
        }

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
