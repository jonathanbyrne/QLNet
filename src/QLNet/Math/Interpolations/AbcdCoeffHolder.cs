using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class AbcdCoeffHolder
    {
        public AbcdCoeffHolder(double? a,
            double? b,
            double? c,
            double? d,
            bool aIsFixed,
            bool bIsFixed,
            bool cIsFixed,
            bool dIsFixed)
        {
            a_ = a;
            b_ = b;
            c_ = c;
            d_ = d;
            aIsFixed_ = false;
            bIsFixed_ = false;
            cIsFixed_ = false;
            dIsFixed_ = false;
            k_ = new List<double>();
            error_ = null;
            maxError_ = null;
            abcdEndCriteria_ = EndCriteria.Type.None;

            if (a_ != null)
            {
                aIsFixed_ = aIsFixed;
            }
            else
            {
                a_ = -0.06;
            }

            if (b_ != null)
            {
                bIsFixed_ = bIsFixed;
            }
            else
            {
                b_ = 0.17;
            }

            if (c_ != null)
            {
                cIsFixed_ = cIsFixed;
            }
            else
            {
                c_ = 0.54;
            }

            if (d_ != null)
            {
                dIsFixed_ = dIsFixed;
            }
            else
            {
                d_ = 0.17;
            }

            AbcdMathFunction.validate(a_.Value, b_.Value, c_.Value, d_.Value);
        }

        public double? a_ { get; set; }

        public EndCriteria.Type abcdEndCriteria_ { get; set; }

        public bool aIsFixed_ { get; set; }

        public double? b_ { get; set; }

        public bool bIsFixed_ { get; set; }

        public double? c_ { get; set; }

        public bool cIsFixed_ { get; set; }

        public double? d_ { get; set; }

        public bool dIsFixed_ { get; set; }

        public double? error_ { get; set; }

        public List<double> k_ { get; set; }

        public double? maxError_ { get; set; }
    }
}
