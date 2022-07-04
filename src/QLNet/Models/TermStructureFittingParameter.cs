using System.Collections.Generic;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.Optimization;
using QLNet.Termstructures;

namespace QLNet.Models
{
    [JetBrains.Annotations.PublicAPI] public class TermStructureFittingParameter : Parameter
    {
        [JetBrains.Annotations.PublicAPI] public class NumericalImpl : Impl
        {
            private List<double> times_;
            private List<double> values_;
            private Handle<YieldTermStructure> termStructure_;

            public NumericalImpl(Handle<YieldTermStructure> termStructure)
            {
                times_ = new List<double>();
                values_ = new List<double>();
                termStructure_ = termStructure;
            }

            public void setvalue(double t, double x)
            {
                times_.Add(t);
                values_.Add(x);
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
            public override double value(Vector UnnamedParameter1, double t)
            {
                var nIndex = times_.FindIndex(val => val.IsEqual(t));
                Utils.QL_REQUIRE(nIndex != -1, () => "fitting parameter not set!");

                return values_[nIndex];
            }

            public Handle<YieldTermStructure> termStructure() => termStructure_;
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