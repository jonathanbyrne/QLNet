using System.Collections.Generic;

namespace QLNet.Methods.Finitedifferences.Meshers
{
    [JetBrains.Annotations.PublicAPI] public class Pair<TFirst, TSecond>
    {
        protected KeyValuePair<TFirst, TSecond> pair;

        public Pair() { }

        public Pair(TFirst first, TSecond second)
        {
            pair = new KeyValuePair<TFirst, TSecond>(first, second);
        }

        public void set(TFirst first, TSecond second)
        {
            pair = new KeyValuePair<TFirst, TSecond>(first, second);
        }

        public TFirst first => pair.Key;

        public TSecond second => pair.Value;
    }
}