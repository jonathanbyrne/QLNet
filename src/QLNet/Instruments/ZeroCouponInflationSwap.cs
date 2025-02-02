﻿/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Time;

namespace QLNet.Instruments
{
    //! Zero-coupon inflation-indexed swap
    /*! Quoted as a fixed rate \f$ K \f$.  At start:
        \f[
        P_n(0,T) N [(1+K)^{T}-1] =
        P_n(0,T) N \left[ \frac{I(T)}{I(0)} -1 \right]
        \f]
        where \f$ T \f$ is the maturity time, \f$ P_n(0,t) \f$ is the
        nominal discount factor at time \f$ t \f$, \f$ N \f$ is the
        notional, and \f$ I(t) \f$ is the inflation index value at
        time \f$ t \f$.

        This inherits from swap and has two very simple legs: a fixed
        leg, from the quote (K); and an indexed leg.  At maturity the
        two single cashflows are swapped.  These are the notional
        versus the inflation-indexed notional Because the coupons are
        zero there are no accruals (and no coupons).

        Inflation is generally available on every day, including
        holidays and weekends.  Hence there is a variable to state
        whether the observe/fix dates for inflation are adjusted or
        not.  The default is not to adjust.

        A zero inflation swap is a simple enough instrument that the
        standard discounting pricing engine that works for a vanilla
        swap also works.

        \note we do not need Schedules on the legs because they use
              one or two dates only per leg.
    */
    [PublicAPI]
    public class ZeroCouponInflationSwap : Swap
    {
        public enum Type
        {
            Receiver = -1,
            Payer = 1
        }

        public new class Arguments : Swap.Arguments
        {
            public double fixedRate { get; set; }
        }

        [PublicAPI]
        public class Engine : GenericEngine<Arguments, Results>
        {
        }

        protected bool adjustInfObsDates_;
        protected Date baseDate_, obsDate_;
        protected DayCounter dayCounter_;
        protected Calendar fixCalendar_;
        protected BusinessDayConvention fixConvention_;
        protected double fixedRate_;
        protected Calendar infCalendar_;
        protected BusinessDayConvention infConvention_;
        protected ZeroInflationIndex infIndex_;
        protected double nominal_;
        protected Period observationLag_;
        protected Date startDate_, maturityDate_;
        protected Type type_;

        /* Generally inflation indices are available with a lag of 1month
           and then observed with a lag of 2-3 months depending whether
           they use an interpolated fixing or not.  Here, we make the
           swap use the interpolation of the index to avoid incompatibilities.
        */
        public ZeroCouponInflationSwap(Type type,
            double nominal,
            Date startDate, // start date of contract (only)
            Date maturity, // this is pre-adjustment!
            Calendar fixCalendar,
            BusinessDayConvention fixConvention,
            DayCounter dayCounter,
            double fixedRate,
            ZeroInflationIndex infIndex,
            Period observationLag,
            bool adjustInfObsDates = false,
            Calendar infCalendar = null,
            BusinessDayConvention? infConvention = null)
            : base(2)
        {
            type_ = type;
            nominal_ = nominal;
            startDate_ = startDate;
            maturityDate_ = maturity;
            fixCalendar_ = fixCalendar;
            fixConvention_ = fixConvention;
            fixedRate_ = fixedRate;
            infIndex_ = infIndex;
            observationLag_ = observationLag;
            adjustInfObsDates_ = adjustInfObsDates;
            infCalendar_ = infCalendar;
            dayCounter_ = dayCounter;

            // first check compatibility of index and swap definitions
            if (infIndex_.interpolated())
            {
                var pShift = new Period(infIndex_.frequency());
                QLNet.Utils.QL_REQUIRE(observationLag_ - pShift > infIndex_.availabilityLag(), () =>
                    "inconsistency between swap observation of index " + observationLag_ +
                    " index availability " + infIndex_.availabilityLag() +
                    " interpolated index period " + pShift +
                    " and index availability " + infIndex_.availabilityLag() +
                    " need (obsLag-index period) > availLag");
            }
            else
            {
                QLNet.Utils.QL_REQUIRE(infIndex_.availabilityLag() < observationLag_, () =>
                    "index tries to observe inflation fixings that do not yet exist: "
                    + " availability lag " + infIndex_.availabilityLag()
                    + " versus obs lag = " + observationLag_);
            }

            if (infCalendar_ == null)
            {
                infCalendar_ = fixCalendar_;
            }

            if (infConvention == null)
            {
                infConvention_ = fixConvention_;
            }
            else
            {
                infConvention_ = infConvention.Value;
            }

            if (adjustInfObsDates_)
            {
                baseDate_ = infCalendar_.adjust(startDate - observationLag_, infConvention_);
                obsDate_ = infCalendar_.adjust(maturity - observationLag_, infConvention_);
            }
            else
            {
                baseDate_ = startDate - observationLag_;
                obsDate_ = maturity - observationLag_;
            }

            var infPayDate = infCalendar_.adjust(maturity, infConvention_);
            var fixedPayDate = fixCalendar_.adjust(maturity, fixConvention_);

            // At this point the index may not be able to forecast
            // i.e. do not want to force the existence of an inflation
            // term structure before allowing users to create instruments.
            var T = Termstructures.Utils.inflationYearFraction(infIndex_.frequency(), infIndex_.interpolated(),
                dayCounter_, baseDate_, obsDate_);
            // N.B. the -1.0 is because swaps only exchange growth, not notionals as well
            var fixedAmount = nominal * (System.Math.Pow(1.0 + fixedRate, T) - 1.0);

            legs_[0].Add(new SimpleCashFlow(fixedAmount, fixedPayDate));
            var growthOnly = true;
            legs_[1].Add(new IndexedCashFlow(nominal, infIndex, baseDate_, obsDate_, infPayDate, growthOnly));

            for (var j = 0; j < 2; ++j)
            {
                legs_[j].ForEach((i, x) => x.registerWith(update));
            }

            switch (type_)
            {
                case Type.Payer:
                    payer_[0] = +1.0;
                    payer_[1] = -1.0;
                    break;
                case Type.Receiver:
                    payer_[0] = -1.0;
                    payer_[1] = +1.0;
                    break;
                default:
                    QLNet.Utils.QL_FAIL("Unknown zero-inflation-swap ExerciseType");
                    break;
            }
        }

        #region Inspectors

        //! "payer" or "receiver" refer to the inflation-indexed leg
        public Type type() => type_;

        public double nominal() => nominal_;

        public new Date startDate() => startDate_;

        public new Date maturityDate() => maturityDate_;

        public Calendar fixedCalendar() => fixCalendar_;

        public BusinessDayConvention fixedConvention() => fixConvention_;

        public DayCounter dayCounter() => dayCounter_;

        //! \f$ K \f$ in the above formula.
        public double fixedRate() => fixedRate_;

        public ZeroInflationIndex inflationIndex() => infIndex_;

        public Period observationLag() => observationLag_;

        public bool adjustObservationDates() => adjustInfObsDates_;

        public Calendar inflationCalendar() => infCalendar_;

        public BusinessDayConvention inflationConvention() => infConvention_;

        //! just one cashflow (that is not a coupon) in each leg
        public List<CashFlow> fixedLeg() => legs_[0];

        //! just one cashflow (that is not a coupon) in each leg
        public List<CashFlow> inflationLeg() => legs_[1];

        #endregion

        #region Instrument interface

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);
            // you don't actually need to do anything else because it is so simple
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);
            // you don't actually need to do anything else because it is so simple
        }

        #endregion

        #region Results

        public double fixedLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[0] != null, () => "result not available");
            return legNPV_[0].Value;
        }

        public double inflationLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[1] != null, () => "result not available");
            return legNPV_[1].Value;
        }

        public double fairRate()
        {
            // What does this mean before or after trade date?
            // Always means that NPV is zero for _this_ instrument
            // if it was created with _this_ rate
            // _knowing_ the time from base to obs (etc).

            var icf = legs_[1][0] as IndexedCashFlow;
            QLNet.Utils.QL_REQUIRE(icf != null, () => "failed to downcast to IndexedCashFlow in ::fairRate()");

            // +1 because the IndexedCashFlow has growthOnly=true
            var growth = icf.amount() / icf.notional() + 1.0;
            var T = Termstructures.Utils.inflationYearFraction(infIndex_.frequency(),
                infIndex_.interpolated(),
                dayCounter_, baseDate_, obsDate_);

            return System.Math.Pow(growth, 1.0 / T) - 1.0;
        }

        #endregion
    }
}
