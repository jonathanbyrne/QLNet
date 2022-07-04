using System;

namespace QLNet.Methods.Finitedifferences.Meshers
{
    [JetBrains.Annotations.PublicAPI] public class pair_double : Pair<double, double>, IComparable<Pair<double, double>>
    {
        public pair_double(double first, double second)
            : base(first, second)
        { }

        public int CompareTo(Pair<double, double> other) => first.CompareTo(other.first);
    }
}