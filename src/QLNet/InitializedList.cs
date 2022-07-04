using System.Collections.Generic;
using QLNet.Patterns;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class InitializedList<T> : List<T> where T : new ()
    {
        public InitializedList() : base() { }
        public InitializedList(int size) : base(size)
        {
            for (var i = 0; i < this.Capacity; i++)
                this.Add(default(T) == null ? FastActivator<T>.Create() : default(T));
        }
        public InitializedList(int size, T value) : base(size)
        {
            for (var i = 0; i < this.Capacity; i++)
                this.Add(value);
        }

        // erases the contents without changing the size
        public void Erase()
        {
            for (var i = 0; i < this.Count; i++)
                this[i] = default(T);       // do we need to use "new T()" instead of default(T) when T is class?
        }
    }
}