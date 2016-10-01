using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Sensus.Tools
{
    public static class EnumerableExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> items)
        {
            return new ObservableCollection<T>(items.ToArray());
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> items, NotifyCollectionChangedEventHandler handler)
        {
            var collection =new ObservableCollection<T>();

            collection.CollectionChanged += handler;

            foreach (var item in items)
            {
                collection.Add(item);
            }

            return collection;
        }
    }
}