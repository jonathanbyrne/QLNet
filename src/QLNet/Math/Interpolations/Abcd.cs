using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class Abcd
    {
        private double a_, b_, c_, d_;
        private bool aIsFixed_, bIsFixed_, cIsFixed_, dIsFixed_;
        private EndCriteria endCriteria_;
        private OptimizationMethod optMethod_;
        private bool vegaWeighted_;

        public Abcd(double a, double b, double c, double d,
            bool aIsFixed, bool bIsFixed,
            bool cIsFixed, bool dIsFixed,
            bool vegaWeighted = false,
            EndCriteria endCriteria = null,
            OptimizationMethod optMethod = null)
        {
            a_ = a;
            b_ = b;
            c_ = c;
            d_ = d;
            aIsFixed_ = aIsFixed;
            bIsFixed_ = bIsFixed;
            cIsFixed_ = cIsFixed;
            dIsFixed_ = dIsFixed;
            vegaWeighted_ = vegaWeighted;
            endCriteria_ = endCriteria;
            optMethod_ = optMethod;
            global = true;
        }

        public bool global { get; set; }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) =>
            new AbcdInterpolation(xBegin, size, yBegin,
                a_, b_, c_, d_,
                aIsFixed_, bIsFixed_,
                cIsFixed_, dIsFixed_,
                vegaWeighted_,
                endCriteria_, optMethod_);
    }
}
