/*
 Copyright (C) 2016 Francois Botha (igitur@gmail.com)

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

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Patterns;
using QLNet.Time;

namespace QLNet.Termstructures.Volatility.equityfx
{
    //! Black volatility surface modelled as variance surface
    /*! This class calculates time/strike dependent Black volatilities
        using as input a matrix of Black volatilities observed in the
        market.

        The calculation is performed interpolating on the variance
        surface.  Bilinear interpolation is used as default; this can
        be changed by the setInterpolation() method.

        \todo check time extrapolation

    */

    [PublicAPI]
    public class BlackVarianceSurface : BlackVarianceTermStructure
    {
        public enum Extrapolation
        {
            ConstantExtrapolation,
            InterpolatorDefaultExtrapolation
        }

        private List<Date> dates_;
        private DayCounter dayCounter_;
        private Extrapolation lowerExtrapolation_, upperExtrapolation_;
        private Date maxDate_;
        private List<double> strikes_;
        private List<double> times_;
        private Matrix variances_;
        private Interpolation2D varianceSurface_;
        private Matrix volatilities_;

        // required for Handle
        public BlackVarianceSurface()
        {
        }

        public BlackVarianceSurface(Date referenceDate,
            Calendar calendar,
            List<Date> dates,
            List<double> strikes,
            Matrix blackVolMatrix,
            DayCounter dayCounter,
            Extrapolation lowerExtrapolation = Extrapolation.InterpolatorDefaultExtrapolation,
            Extrapolation upperExtrapolation = Extrapolation.InterpolatorDefaultExtrapolation)
            : base(referenceDate, calendar)
        {
            dayCounter_ = dayCounter;
            maxDate_ = dates.Last();
            strikes_ = strikes;
            lowerExtrapolation_ = lowerExtrapolation;
            upperExtrapolation_ = upperExtrapolation;
            dates_ = dates;
            volatilities_ = blackVolMatrix;

            QLNet.Utils.QL_REQUIRE(dates.Count == blackVolMatrix.columns(), () =>
                "mismatch between date vector and vol matrix colums");
            QLNet.Utils.QL_REQUIRE(strikes_.Count == blackVolMatrix.rows(), () =>
                "mismatch between money-strike vector and vol matrix rows");
            QLNet.Utils.QL_REQUIRE(dates[0] >= referenceDate, () =>
                "cannot have dates[0] < referenceDate");

            int i, j;
            times_ = new InitializedList<double>(dates.Count + 1);
            times_[0] = 0.0;
            variances_ = new Matrix(strikes_.Count, dates.Count + 1);
            for (i = 0; i < blackVolMatrix.rows(); i++)
            {
                variances_[i, 0] = 0.0;
            }

            for (j = 1; j <= blackVolMatrix.columns(); j++)
            {
                times_[j] = timeFromReference(dates[j - 1]);
                QLNet.Utils.QL_REQUIRE(times_[j] > times_[j - 1],
                    () => "dates must be sorted unique!");
                for (i = 0; i < blackVolMatrix.rows(); i++)
                {
                    variances_[i, j] = times_[j] * blackVolMatrix[i, j - 1] * blackVolMatrix[i, j - 1];
                }
            }

            // default: bilinear interpolation
            setInterpolation<Bilinear>();
        }

        public virtual List<Date> dates() => dates_;

        // TermStructure interface
        public override DayCounter dayCounter() => dayCounter_;

        public override Date maxDate() => maxDate_;

        public override double maxStrike() => strikes_.Last();

        // VolatilityTermStructure interface
        public override double minStrike() => strikes_.First();

        public void setInterpolation<Interpolator>() where Interpolator : IInterpolationFactory2D, new()
        {
            setInterpolation(FastActivator<Interpolator>.Create());
        }

        public void setInterpolation<Interpolator>(Interpolator i) where Interpolator : IInterpolationFactory2D, new()
        {
            varianceSurface_ = i.interpolate(times_, times_.Count, strikes_, strikes_.Count, variances_);
            varianceSurface_.update();
            notifyObservers();
        }

        //public accessors
        public virtual List<double> strikes() => strikes_;

        public virtual List<double> times() => times_;

        public virtual Matrix variances() => variances_;

        public virtual Matrix volatilities() => volatilities_;

        protected override double blackVarianceImpl(double t, double strike)
        {
            if (t.IsEqual(0.0))
            {
                return 0.0;
            }

            // enforce constant extrapolation when required
            if (strike < strikes_.First() && lowerExtrapolation_ == Extrapolation.ConstantExtrapolation)
            {
                strike = strikes_.First();
            }

            if (strike > strikes_.Last() && upperExtrapolation_ == Extrapolation.ConstantExtrapolation)
            {
                strike = strikes_.Last();
            }

            if (t <= times_.Last())
            {
                return varianceSurface_.value(t, strike, true);
            }

            return varianceSurface_.value(times_.Last(), strike, true) * t / times_.Last();
        }
    }
}
