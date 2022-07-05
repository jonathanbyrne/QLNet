using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class TimeSeries<T> : IDictionary<Date, T>
    {
        private Dictionary<Date, T> backingDictionary_;

        // constructors
        public TimeSeries()
        {
            backingDictionary_ = new Dictionary<Date, T>();
        }

        public TimeSeries(int size)
        {
            backingDictionary_ = new Dictionary<Date, T>(size);
        }

        public T this[Date key]
        {
            get
            {
                if (backingDictionary_.ContainsKey(key))
                {
                    return backingDictionary_[key];
                }

                return default(T);
            }
            set => backingDictionary_[key] = value;
        }

        public int Count => backingDictionary_.Count;

        public bool IsReadOnly => false;

        public ICollection<Date> Keys => backingDictionary_.Keys;

        public ICollection<T> Values => backingDictionary_.Values;

        public void Add(KeyValuePair<Date, T> item)
        {
            backingDictionary_.Add(item.Key, item.Value);
        }

        public void Add(Date key, T value)
        {
            backingDictionary_.Add(key, value);
        }

        public void Clear()
        {
            backingDictionary_.Clear();
        }

        public bool Contains(KeyValuePair<Date, T> item) => backingDictionary_.Contains(item);

        public bool ContainsKey(Date key) => backingDictionary_.ContainsKey(key);

        public void CopyTo(KeyValuePair<Date, T>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<Date, T>> GetEnumerator() => backingDictionary_.GetEnumerator();

        public bool Remove(KeyValuePair<Date, T> item) => backingDictionary_.Remove(item.Key);

        public bool Remove(Date key) => backingDictionary_.Remove(key);

        public bool TryGetValue(Date key, out T value) => backingDictionary_.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => backingDictionary_.GetEnumerator();
    }
}
