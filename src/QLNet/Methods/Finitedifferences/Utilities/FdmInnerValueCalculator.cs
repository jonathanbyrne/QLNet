/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using QLNet.Instruments;
using QLNet.Math;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using QLNet.Math.integrals;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    public abstract class FdmInnerValueCalculator
    {
        public abstract double innerValue(FdmLinearOpIterator iter, double t);
        public abstract double avgInnerValue(FdmLinearOpIterator iter, double t);
    }

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

    [JetBrains.Annotations.PublicAPI] public class FdmLogBasketInnerValue : FdmInnerValueCalculator
    {
        public FdmLogBasketInnerValue(BasketPayoff payoff,
                                      FdmMesher mesher)
        {
            payoff_ = payoff;
            mesher_ = mesher;
        }

        public override double innerValue(FdmLinearOpIterator iter, double t)
        {
            var x = new Vector(mesher_.layout().dim().Count);
            for (var i = 0; i < x.size(); ++i)
            {
                x[i] = System.Math.Exp(mesher_.location(iter, i));
            }

            return payoff_.value(x);
        }
        public override double avgInnerValue(FdmLinearOpIterator iter, double t) => innerValue(iter, t);

        protected BasketPayoff payoff_;
        protected FdmMesher mesher_;
    }

    [JetBrains.Annotations.PublicAPI] public class FdmZeroInnerValue : FdmInnerValueCalculator
    {
        public override double innerValue(FdmLinearOpIterator iter, double t) => 0.0;

        public override double avgInnerValue(FdmLinearOpIterator iter, double t) => 0.0;
    }
}
