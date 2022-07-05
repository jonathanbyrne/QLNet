//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    // mixed interpolation between discrete points

    [PublicAPI]
    public class MixedInterpolationImpl<Interpolator1, Interpolator2> : Interpolation.templateImpl
        where Interpolator1 : IInterpolationFactory, new()
        where Interpolator2 : IInterpolationFactory, new()
    {
        private Interpolation interpolation1_, interpolation2_;
        private int n_;
        private List<double> xBegin2_;
        private List<double> yBegin2_;

        public MixedInterpolationImpl(List<double> xBegin, int xEnd,
            List<double> yBegin, int n,
            Behavior behavior = Behavior.ShareRanges,
            Interpolator1 factory1 = default,
            Interpolator2 factory2 = default)
            : base(xBegin, xEnd, yBegin,
                System.Math.Max(factory1 == null ? (factory1 = new Interpolator1()).requiredPoints : factory1.requiredPoints,
                    factory2 == null ? (factory2 = new Interpolator2()).requiredPoints : factory2.requiredPoints))
        {
            n_ = n;

            xBegin2_ = xBegin.GetRange(n_, xBegin.Count);
            yBegin2_ = yBegin.GetRange(n_, yBegin.Count);

            QLNet.Utils.QL_REQUIRE(xBegin2_.Count < size_, () => "too large n (" + n + ") for " + size_ + "-element x sequence");

            switch (behavior)
            {
                case Behavior.ShareRanges:
                    interpolation1_ = factory1.interpolate(xBegin_, size_, yBegin_);
                    interpolation2_ = factory2.interpolate(xBegin_, size_, yBegin_);
                    break;
                case Behavior.SplitRanges:
                    interpolation1_ = factory1.interpolate(xBegin_, xBegin2_.Count + 1, yBegin_);
                    interpolation2_ = factory2.interpolate(xBegin2_, size_, yBegin2_);
                    break;
                default:
                    QLNet.Utils.QL_FAIL("unknown mixed-interpolation behavior: " + behavior);
                    break;
            }
        }

        public override double derivative(double x)
        {
            if (x < xBegin2_.First())
            {
                return interpolation1_.derivative(x, true);
            }

            return interpolation2_.derivative(x, true);
        }

        public override double primitive(double x)
        {
            if (x < xBegin2_.First())
            {
                return interpolation1_.primitive(x, true);
            }

            return interpolation2_.primitive(x, true) -
                   interpolation2_.primitive(xBegin2_.First(), true) +
                   interpolation1_.primitive(xBegin2_.First(), true);
        }

        public override double secondDerivative(double x)
        {
            if (x < xBegin2_.First())
            {
                return interpolation1_.secondDerivative(x, true);
            }

            return interpolation2_.secondDerivative(x, true);
        }

        public int switchIndex() => n_;

        public override void update()
        {
            interpolation1_.update();
            interpolation2_.update();
        }

        public override double value(double x)
        {
            if (x < xBegin2_.First())
            {
                return interpolation1_.value(x, true);
            }

            return interpolation2_.value(x, true);
        }
    }

    //! mixed linear/cubic interpolation between discrete points

    //! mixed linear/cubic interpolation factory and traits
    /*! \ingroup interpolations */

    // convenience classes
}
