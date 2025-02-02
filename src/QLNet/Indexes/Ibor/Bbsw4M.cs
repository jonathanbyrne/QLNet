﻿using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Bbsw4M : Bbsw
    {
        public Bbsw4M(Handle<YieldTermStructure> h = null)
            : base(new Period(4, TimeUnit.Months), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
