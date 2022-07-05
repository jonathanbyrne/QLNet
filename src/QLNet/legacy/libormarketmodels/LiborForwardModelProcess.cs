/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Math;
using QLNet.processes;
using QLNet.Time;

namespace QLNet.legacy.libormarketmodels
{
    [PublicAPI]
    public class LiborForwardModelProcess : StochasticProcess
    {
        private List<double> accrualEndTimes_;
        private Vector m1;
        private Vector m2;

        public LiborForwardModelProcess(int size, IborIndex index, IDiscretization disc)
            : base(disc)
        {
            size_ = size;
            index_ = index;
            initialValues_ = new InitializedList<double>(size_);
            fixingTimes_ = new InitializedList<double>(size);
            fixingDates_ = new InitializedList<Date>(size_);
            accrualStartTimes_ = new InitializedList<double>(size);
            accrualEndTimes_ = new InitializedList<double>(size);
            accrualPeriod_ = new InitializedList<double>(size_);
            m1 = new Vector(size_);
            m2 = new Vector(size_);
            var dayCounter = index.dayCounter();
            IList<CashFlow> flows = cashFlows(1);

            Utils.QL_REQUIRE(size_ == flows.Count, () => "wrong number of cashflows");

            var settlement = index_.forwardingTermStructure().link.referenceDate();
            Date startDate;
            var iborcoupon = (IborCoupon)flows[0];
            startDate = iborcoupon.fixingDate();

            for (var i = 0; i < size_; ++i)
            {
                var coupon = (IborCoupon)flows[i];

                Utils.QL_REQUIRE(coupon.date() == coupon.accrualEndDate(), () => "irregular coupon types are not suppported");

                initialValues_[i] = coupon.rate();
                accrualPeriod_[i] = coupon.accrualPeriod();

                fixingDates_[i] = coupon.fixingDate();
                fixingTimes_[i] = dayCounter.yearFraction(startDate, coupon.fixingDate());
                accrualStartTimes_[i] = dayCounter.yearFraction(settlement, coupon.accrualStartDate());
                accrualEndTimes_[i] = dayCounter.yearFraction(settlement, coupon.accrualEndDate());
            }
        }

        public LiborForwardModelProcess(int size, IborIndex index)
            : this(size, index, new EulerDiscretization())
        {
        }

        public List<double> accrualPeriod_ { get; set; }

        public List<double> accrualStartTimes_ { get; set; }

        public List<Date> fixingDates_ { get; set; }

        public List<double> fixingTimes_ { get; set; }

        public IborIndex index_ { get; set; }

        public List<double> initialValues_ { get; set; }

        public LfmCovarianceParameterization lfmParam_ { get; set; }

        public int size_ { get; set; }

        public List<double> accrualEndTimes() => accrualEndTimes_;

        public List<double> accrualStartTimes() => accrualStartTimes_;

        public override Vector apply(Vector x0, Vector dx)
        {
            var tmp = new Vector(size_);
            for (var k = 0; k < size_; ++k)
            {
                tmp[k] = x0[k] * System.Math.Exp(dx[k]);
            }

            return tmp;
        }

        public List<CashFlow> cashFlows() => cashFlows(1);

        public List<CashFlow> cashFlows(double amount)
        {
            var refDate = index_.forwardingTermStructure().link.referenceDate();

            var schedule = new Schedule(refDate,
                refDate + new Period(index_.tenor().length() * size_,
                    index_.tenor().units()),
                index_.tenor(), index_.fixingCalendar(),
                index_.businessDayConvention(),
                index_.businessDayConvention(),
                DateGeneration.Rule.Forward, false);

            var cashflows = (IborLeg)new IborLeg(schedule, index_)
                .withFixingDays(index_.fixingDays())
                .withPaymentDayCounter(index_.dayCounter())
                .withNotionals(amount)
                .withPaymentAdjustment(index_.businessDayConvention());
            return cashflows.value();
        }

        public override Matrix covariance(double t, Vector x, double dt) => lfmParam_.covariance(t, x) * dt;

        public LfmCovarianceParameterization covarParam() => lfmParam_;

        public override Matrix diffusion(double t, Vector x) => lfmParam_.diffusion(t, x);

        public List<double> discountBond(List<double> rates)
        {
            List<double> discountFactors = new InitializedList<double>(size_);
            discountFactors[0] = 1.0 / (1.0 + rates[0] * accrualPeriod_[0]);

            for (var i = 1; i < size_; ++i)
            {
                discountFactors[i] =
                    discountFactors[i - 1] / (1.0 + rates[i] * accrualPeriod_[i]);
            }

            return discountFactors;
        }

        public override Vector drift(double t, Vector x)
        {
            var f = new Vector(size_, 0.0);
            var covariance = lfmParam_.covariance(t, x);
            var m = nextIndexReset(t);

            for (var k = m; k < size_; ++k)
            {
                m1[k] = accrualPeriod_[k] * x[k] / (1 + accrualPeriod_[k] * x[k]);
                double inner_product = 0;
                m1.GetRange(m, k + 1 - m).ForEach(
                    (ii, vv) => inner_product += vv *
                                                 covariance.column(k).GetRange(m, covariance.rows() - m)[ii]);

                f[k] = inner_product - 0.5 * covariance[k, k];
            }

            return f;
        }

        public override Vector evolve(double t0, Vector x0, double dt, Vector dw)
        {
            // predictor-corrector step to reduce discretization errors.

            var m = nextIndexReset(t0);
            var sdt = System.Math.Sqrt(dt);

            var f = new Vector(x0);
            var diff = lfmParam_.diffusion(t0, x0);
            var covariance = lfmParam_.covariance(t0, x0);

            for (var k = m; k < size_; ++k)
            {
                var y = accrualPeriod_[k] * x0[k];
                m1[k] = y / (1 + y);

                double d = 0;
                m1.GetRange(m, k + 1 - m).ForEach(
                    (ii, vv) => d += vv *
                                     covariance.column(k).GetRange(m, covariance.rows() - m)[ii]);
                d = (d - 0.5 * covariance[k, k]) * dt;

                double r = 0;
                diff.row(k).ForEach((kk, vv) => r += vv * dw[kk]);
                r *= sdt;

                var x = y * System.Math.Exp(d + r);
                m2[k] = x / (1 + x);

                double inner_product = 0;
                m2.GetRange(m, k + 1 - m).ForEach(
                    (ii, vv) => inner_product += vv *
                                                 covariance.column(k).GetRange(m, covariance.rows() - m)[ii]);
                f[k] = x0[k] * System.Math.Exp(0.5 * (d + (inner_product - 0.5 * covariance[k, k]) * dt) + r);
            }

            return f;
        }

        public override int factors() => lfmParam_.factors();

        public List<Date> fixingDates() => fixingDates_;

        public List<double> fixingTimes() => fixingTimes_;

        public IborIndex index() => index_;

        public override Vector initialValues()
        {
            var tmp = new Vector(size());
            for (var i = 0; i < size(); ++i)
            {
                tmp[i] = initialValues_[i];
            }

            return tmp;
        }

        public int nextIndexReset(double t)
        {
            var result = fixingTimes_.FindIndex(x => x > t);
            if (result < 0)
            {
                result = ~result - 1;
            }

            // impose limits. we need the one before last at max or the first at min
            result = System.Math.Max(System.Math.Min(result, fixingTimes_.Count - 1), 0);
            return result;
        }

        public void setCovarParam(LfmCovarianceParameterization param)
        {
            lfmParam_ = param;
        }

        public override int size() => size_;
    }
}
