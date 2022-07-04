using System;
using System.Collections.Generic;
using QLNet.Math.integrals;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    [JetBrains.Annotations.PublicAPI] public class FdmLogInnerValue : FdmInnerValueCalculator
    {
        public FdmLogInnerValue(Payoff payoff,
            FdmMesher mesher,
            int direction)
        {
            payoff_ = payoff;
            mesher_ = mesher;
            direction_ = direction;
            avgInnerValues_ = new List<double>();
        }

        public override double innerValue(FdmLinearOpIterator iter, double t)
        {
            var s = System.Math.Exp(mesher_.location(iter, direction_));
            return payoff_.value(s);
        }
        public override double avgInnerValue(FdmLinearOpIterator iter, double t)
        {
            if (avgInnerValues_.empty())
            {
                // calculate caching values
                avgInnerValues_ = new InitializedList<double>(mesher_.layout().dim()[direction_]);
                List<bool> initialized = new InitializedList<bool>(avgInnerValues_.Count, false);

                var layout = mesher_.layout();
                var endIter = layout.end();
                for (var new_iter = layout.begin(); new_iter != endIter;
                     ++new_iter)
                {
                    var xn = new_iter.coordinates()[direction_];
                    if (!initialized[xn])
                    {
                        initialized[xn] = true;
                        avgInnerValues_[xn] = avgInnerValueCalc(new_iter, t);
                    }
                }
            }
            return avgInnerValues_[iter.coordinates()[direction_]];
        }

        protected double avgInnerValueCalc(FdmLinearOpIterator iter, double t)
        {
            var dim = mesher_.layout().dim()[direction_];
            var coord = iter.coordinates()[direction_];
            var loc = mesher_.location(iter, direction_);
            var a = loc;
            var b = loc;
            if (coord > 0)
            {
                a -= mesher_.dminus(iter, direction_).Value / 2.0;
            }
            if (coord < dim - 1)
            {
                b += mesher_.dplus(iter, direction_).Value / 2.0;
            }
            Func<double, double> f = x => payoff_.value(System.Math.Exp(x));
            double retVal;
            try
            {
                var acc
                    = f(a) != 0.0 || f(b) != 0.0 ? (f(a) + f(b)) * 5e-5 : 1e-4;
                retVal = new SimpsonIntegral(acc, 8).value(f, a, b) / (b - a);
            }
            catch
            {
                // use default value
                retVal = innerValue(iter, t);
            }

            return retVal;
        }

        protected Payoff payoff_;
        protected FdmMesher mesher_;
        protected int direction_;
        protected List<double> avgInnerValues_;
    }
}