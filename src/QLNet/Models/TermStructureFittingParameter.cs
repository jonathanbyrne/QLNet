using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.Optimization;
using QLNet.Termstructures;

namespace QLNet.Models
{
    [PublicAPI]
    public class TermStructureFittingParameter : Parameter
    {
        [PublicAPI]
        public class NumericalImpl : Impl
        {
            private Handle<YieldTermStructure> termStructure_;
            private List<double> times_;
            private List<double> values_;

            public NumericalImpl(Handle<YieldTermStructure> termStructure)
            {
                times_ = new List<double>();
                values_ = new List<double>();
                termStructure_ = termStructure;
            }

            public void change(double x)
            {
                values_[values_.Count - 1] = x;
            }

            public void reset()
            {
                times_.Clear();
                values_.Clear();
            }

            public void setvalue(double t, double x)
            {
                times_.Add(t);
                values_.Add(x);
            }

            public Handle<YieldTermStructure> termStructure() => termStructure_;

            public override double value(Vector UnnamedParameter1, double t)
            {
                var nIndex = times_.FindIndex(val => val.IsEqual(t));
                Utils.QL_REQUIRE(nIndex != -1, () => "fitting parameter not set!");

                return values_[nIndex];
            }
        }

        public TermStructureFittingParameter(Impl impl)
            : base(0, impl, new NoConstraint())
        {
        }

        public TermStructureFittingParameter(Handle<YieldTermStructure> term)
            : base(0, new NumericalImpl(term), new NoConstraint())
        {
        }
    }
}
