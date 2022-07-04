using System.Collections.Generic;
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class Abcd
    {
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

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) =>
            new AbcdInterpolation(xBegin, size, yBegin,
                a_, b_, c_, d_,
                aIsFixed_, bIsFixed_,
                cIsFixed_, dIsFixed_,
                vegaWeighted_,
                endCriteria_, optMethod_);

        public bool global { get; set; }

        private double a_, b_, c_, d_;
        private bool aIsFixed_, bIsFixed_, cIsFixed_, dIsFixed_;
        private bool vegaWeighted_;
        private EndCriteria endCriteria_;
        private OptimizationMethod optMethod_;
    }
}