using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class TimeSeries<T> : IDictionary<Date, T>
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

        IEnumerator IEnumerable.GetEnumerator() => backingDictionary_.GetEnumerator();

        public IEnumerator<KeyValuePair<Date, T>> GetEnumerator() => backingDictionary_.GetEnumerator();

        public void Add(KeyValuePair<Date, T> item)
        {
            backingDictionary_.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            backingDictionary_.Clear();
        }

        public bool Contains(KeyValuePair<Date, T> item) => backingDictionary_.Contains(item);

        public void CopyTo(KeyValuePair<Date, T>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<Date, T> item) => backingDictionary_.Remove(item.Key);

        public int Count => backingDictionary_.Count;

        public bool IsReadOnly => false;

        public bool ContainsKey(Date key) => backingDictionary_.ContainsKey(key);

        public void Add(Date key, T value)
        {
            backingDictionary_.Add(key, value);
        }

        public bool Remove(Date key) => backingDictionary_.Remove(key);

        public bool TryGetValue(Date key, out T value) => backingDictionary_.TryGetValue(key, out value);

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

        public ICollection<Date> Keys => backingDictionary_.Keys;

        public ICollection<T> Values => backingDictionary_.Values;
    }
}