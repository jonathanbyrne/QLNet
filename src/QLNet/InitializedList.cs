using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet
{
    [PublicAPI]
    public class InitializedList<T> : List<T> where T : new()
    {
        public InitializedList()
        {
        }

        public InitializedList(int size) : base(size)
        {
            for (var i = 0; i < Capacity; i++)
            {
                Add(default(T) == null ? FastActivator<T>.Create() : default(T));
            }
        }

        public InitializedList(int size, T value) : base(size)
        {
            for (var i = 0; i < Capacity; i++)
            {
                Add(value);
            }
        }

        // erases the contents without changing the size
        public void Erase()
        {
            for (var i = 0; i < Count; i++)
            {
                this[i] = default(T); // do we need to use "new T()" instead of default(T) when T is class?
            }
        }
    }
}
