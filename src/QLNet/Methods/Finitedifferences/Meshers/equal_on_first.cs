using System;
using System.Collections.Generic;

namespace QLNet.Methods.Finitedifferences.Meshers
{
    [JetBrains.Annotations.PublicAPI] public class equal_on_first : IEqualityComparer<Pair<double?, double?>>
    {
        public bool Equals(Pair<double?, double?> p1,
            Pair<double?, double?> p2) =>
            Utils.close_enough(p1.first.Value, p2.first.Value, 1000);

        public int GetHashCode(Pair<double?, double?> p) => Convert.ToInt32(p.first.Value * p.second.Value);
    }
}