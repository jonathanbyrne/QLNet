﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)

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

using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Math.Optimization;
using QLNet.Time;

namespace QLNet.Termstructures
{
    // penalty function class for solving using a multi-dimensional solver

    //! Localised-term-structure bootstrapper for most curve types.
    /*! This algorithm enables a localised fitting for non-local
        interpolation methods.

        As in the similar class (IterativeBootstrap) the input term
        structure is solved on a number of market instruments which
        are passed as a vector of handles to BootstrapHelper
        instances. Their maturities mark the boundaries of the
        interpolated segments.

        Unlike the IterativeBootstrap class, the solution for each
        interpolated segment is derived using a local
        approximation. This restricts the risk profile s.t.  the risk
        is localised. Therefore, we obtain a local IR risk profile
        whilst using a smoother interpolation method. Particularly
        good for the convex-monotone spline method.
    */
    [PublicAPI]
    public class LocalBootstrap<T, U> : IBootStrap<T>
        where T : Curve<U>, new()
        where U : TermStructure
    {
        private bool forcePositive_;
        private int localisation_;
        private T ts_; // yes, it is a workaround
        private bool validCurve_;

        public LocalBootstrap() : this(2, true)
        {
        }

        public LocalBootstrap(int localisation, bool forcePositive)
        {
            localisation_ = localisation;
            forcePositive_ = forcePositive;
        }

        public void calculate()
        {
            validCurve_ = false;
            int nInsts = ts_.instruments_.Count, i;

            // ensure rate helpers are sorted
            ts_.instruments_.Sort((x, y) => x.latestDate().CompareTo(y.latestDate()));

            // check that there is no instruments with the same maturity
            for (i = 1; i < nInsts; ++i)
            {
                Date m1 = ts_.instruments_[i - 1].latestDate(),
                    m2 = ts_.instruments_[i].latestDate();
                QLNet.Utils.QL_REQUIRE(m1 != m2, () => "two instruments have the same maturity (" + m1 + ")");
            }

            // check that there is no instruments with invalid quote
            QLNet.Utils.QL_REQUIRE((i = ts_.instruments_.FindIndex(x => !x.quoteIsValid())) == -1, () =>
                "instrument " + i + " (maturity: " + ts_.instruments_[i].latestDate() + ") has an invalid quote");

            // setup instruments and register with them
            ts_.instruments_.ForEach((x, j) => ts_.setTermStructure(j));

            // set initial guess only if the current curve cannot be used as guess
            if (validCurve_)
            {
                QLNet.Utils.QL_REQUIRE(ts_.data_.Count == nInsts + 1, () =>
                    "dimension mismatch: expected " + nInsts + 1 + ", actual " + ts_.data_.Count);
            }
            else
            {
                ts_.data_ = new InitializedList<double>(nInsts + 1);
                ts_.data_[0] = ts_.initialValue();
            }

            // calculate dates and times
            ts_.dates_ = new InitializedList<Date>(nInsts + 1);
            ts_.times_ = new InitializedList<double>(nInsts + 1);
            ts_.dates_[0] = ts_.initialDate();
            ts_.times_[0] = ts_.timeFromReference(ts_.dates_[0]);
            for (i = 0; i < nInsts; ++i)
            {
                ts_.dates_[i + 1] = ts_.instruments_[i].latestDate();
                ts_.times_[i + 1] = ts_.timeFromReference(ts_.dates_[i + 1]);
                if (!validCurve_)
                {
                    ts_.data_[i + 1] = ts_.data_[i];
                }
            }

            var solver = new LevenbergMarquardt(ts_.accuracy_, ts_.accuracy_, ts_.accuracy_);
            var endCriteria = new EndCriteria(100, 10, 0.00, ts_.accuracy_, 0.00);
            var posConstraint = new PositiveConstraint();
            var noConstraint = new NoConstraint();
            var solverConstraint = forcePositive_ ? posConstraint : (Constraint)noConstraint;

            // now start the bootstrapping.
            var iInst = localisation_ - 1;

            var dataAdjust = (ts_.interpolator_ as ConvexMonotone).dataSizeAdjustment;

            do
            {
                var initialDataPt = iInst + 1 - localisation_ + dataAdjust;
                var startArray = new Vector(localisation_ + 1 - dataAdjust);
                for (var j = 0; j < startArray.size() - 1; ++j)
                {
                    startArray[j] = ts_.data_[initialDataPt + j];
                }

                // here we are extending the interpolation a point at a
                // time... but the local interpolator can make an
                // approximation for the final localisation period.
                // e.g. if the localisation is 2, then the first section
                // of the curve will be solved using the first 2
                // instruments... with the local interpolator making
                // suitable boundary conditions.
                ts_.interpolation_ = (ts_.interpolator_ as ConvexMonotone).localInterpolate(ts_.times_, iInst + 2, ts_.data_,
                    localisation_, ts_.interpolation_ as ConvexMonotoneInterpolation, nInsts + 1);

                if (iInst >= localisation_)
                {
                    startArray[localisation_ - dataAdjust] = ts_.guess(iInst, ts_, false, 0);
                }
                else
                {
                    startArray[localisation_ - dataAdjust] = ts_.data_[0];
                }

                var currentCost = new PenaltyFunction<T, U>(ts_, initialDataPt, ts_.instruments_,
                    iInst - localisation_ + 1, iInst + 1);
                var toSolve = new Problem(currentCost, solverConstraint, startArray);
                var endType = solver.minimize(toSolve, endCriteria);

                // check the end criteria
                QLNet.Utils.QL_REQUIRE(endType == EndCriteria.Type.StationaryFunctionAccuracy ||
                                 endType == EndCriteria.Type.StationaryFunctionValue, () =>
                    "Unable to strip yieldcurve to required accuracy ");
                ++iInst;
            } while (iInst < nInsts);

            validCurve_ = true;
        }

        public void setup(T ts)
        {
            ts_ = ts;

            var n = ts_.instruments_.Count;
            QLNet.Utils.QL_REQUIRE(n >= ts_.interpolator_.requiredPoints, () =>
                "not enough instruments: " + n + " provided, " + ts_.interpolator_.requiredPoints + " required");

            QLNet.Utils.QL_REQUIRE(n > localisation_, () =>
                "not enough instruments: " + n + " provided, " + localisation_ + " required.");

            ts_.instruments_.ForEach((i, x) => ts_.registerWith(x));
        }
    }
}
