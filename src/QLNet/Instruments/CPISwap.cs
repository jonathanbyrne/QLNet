﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! zero-inflation-indexed swap,
    /*! fixed x zero-inflation, i.e. fixed x CPI(i'th fixing)/CPI(base)
        versus floating + spread

        Note that this does ony the inflation-vs-floating-leg.
        Extension to inflation-vs-fixed-leg.  is simple - just replace
        the floating leg with a fixed leg.

        Typically there are notional exchanges at the end: either
        inflated-notional vs notional; or just (inflated-notional -
        notional) vs zero.  The latter is perhaphs more typical.
        \warning Setting subtractInflationNominal to true means that
        the original inflation nominal is subtracted from both
        nominals before they are exchanged, even if they are
        different.

        This swap can mimic a ZCIIS where [(1+q)^n - 1] is exchanged
        against (cpi ratio - 1), by using differnt nominals on each
        leg and setting subtractInflationNominal to true.  ALSO -
        there must be just one date in each schedule.

        The two legs can have different schedules, fixing (days vs
        lag), settlement, and roll conventions.  N.B. accrual
        adjustment periods are already in the schedules.  Trade date
        and swap settlement date are outside the scope of the
        instrument.
    */
    [PublicAPI]
    public class CPISwap : Swap
    {
        public enum Type
        {
            Receiver = -1,
            Payer = 1
        }

        public new class Arguments : Swap.Arguments
        {
            public Arguments()
            {
                type = Type.Receiver;
                nominal = null;
            }

            public double? nominal { get; set; }

            public Type type { get; set; }
        }

        [PublicAPI]
        public class Engine : GenericEngine<Arguments, Results>
        {
        }

        public new class Results : Swap.Results
        {
            public double? fairRate { get; set; }

            public double? fairSpread { get; set; }

            public override void reset()
            {
                base.reset();
                fairRate = null;
                fairSpread = null;
            }
        }

        private double baseCPI_;
        private double? fairRate_;
        // results
        private double? fairSpread_;
        private DayCounter fixedDayCount_;
        private ZeroInflationIndex fixedIndex_;
        private BusinessDayConvention fixedPaymentRoll_;

        // fixed x inflation leg
        private double fixedRate_;
        private Schedule fixedSchedule_;
        private int fixingDays_;
        private DayCounter floatDayCount_;
        private IborIndex floatIndex_;
        private BusinessDayConvention floatPaymentRoll_;
        private Schedule floatSchedule_;
        private double inflationNominal_;
        private double nominal_;
        private InterpolationType observationInterpolation_;
        private Period observationLag_;

        // float+spread leg
        private double spread_;
        private bool subtractInflationNominal_;
        private Type type_;

        public CPISwap(Type type,
            double nominal,
            bool subtractInflationNominal,
            // float+spread leg
            double spread,
            DayCounter floatDayCount,
            Schedule floatSchedule,
            BusinessDayConvention floatPaymentRoll,
            int fixingDays,
            IborIndex floatIndex,
            // fixed x inflation leg
            double fixedRate,
            double baseCPI,
            DayCounter fixedDayCount,
            Schedule fixedSchedule,
            BusinessDayConvention fixedPaymentRoll,
            Period observationLag,
            ZeroInflationIndex fixedIndex,
            InterpolationType observationInterpolation = InterpolationType.AsIndex,
            double? inflationNominal = null)
            : base(2)
        {
            type_ = type;
            nominal_ = nominal;
            subtractInflationNominal_ = subtractInflationNominal;
            spread_ = spread;
            floatDayCount_ = floatDayCount;
            floatSchedule_ = floatSchedule;
            floatPaymentRoll_ = floatPaymentRoll;
            fixingDays_ = fixingDays;
            floatIndex_ = floatIndex;
            fixedRate_ = fixedRate;
            baseCPI_ = baseCPI;
            fixedDayCount_ = fixedDayCount;
            fixedSchedule_ = fixedSchedule;
            fixedPaymentRoll_ = fixedPaymentRoll;
            fixedIndex_ = fixedIndex;
            observationLag_ = observationLag;
            observationInterpolation_ = observationInterpolation;

            QLNet.Utils.QL_REQUIRE(floatSchedule_.Count > 0, () => "empty float schedule");
            QLNet.Utils.QL_REQUIRE(fixedSchedule_.Count > 0, () => "empty fixed schedule");
            // todo if roll!=unadjusted then need calendars ...

            inflationNominal_ = inflationNominal ?? nominal_;

            List<CashFlow> floatingLeg;
            if (floatSchedule_.Count > 1)
            {
                floatingLeg = new IborLeg(floatSchedule_, floatIndex_)
                    .withFixingDays(fixingDays_)
                    .withPaymentDayCounter(floatDayCount_)
                    .withSpreads(spread_)
                    .withNotionals(nominal_)
                    .withPaymentAdjustment(floatPaymentRoll_);
            }
            else
            {
                floatingLeg = new List<CashFlow>();
            }

            if (floatSchedule_.Count == 1 ||
                !subtractInflationNominal_ ||
                subtractInflationNominal && System.Math.Abs(nominal_ - inflationNominal_) > 0.00001
               )
            {
                Date payNotional;
                if (floatSchedule_.Count == 1)
                {
                    // no coupons
                    payNotional = floatSchedule_[0];
                    payNotional = floatSchedule_.calendar().adjust(payNotional, floatPaymentRoll_);
                }
                else
                {
                    // use the pay date of the last coupon
                    payNotional = floatingLeg.Last().date();
                }

                var floatAmount = subtractInflationNominal_ ? nominal_ - inflationNominal_ : nominal_;
                CashFlow nf = new SimpleCashFlow(floatAmount, payNotional);
                floatingLeg.Add(nf);
            }

            // a CPIleg know about zero legs and inclusion of base inflation notional
            List<CashFlow> cpiLeg = new CPILeg(fixedSchedule_, fixedIndex_, baseCPI_, observationLag_)
                .withFixedRates(fixedRate_)
                .withPaymentDayCounter(fixedDayCount_)
                .withObservationInterpolation(observationInterpolation_)
                .withSubtractInflationNominal(subtractInflationNominal_)
                .withNotionals(inflationNominal_)
                .withPaymentAdjustment(fixedPaymentRoll_);

            foreach (var cashFlow in cpiLeg)
            {
                cashFlow.registerWith(update);
            }

            if (floatingLeg.Count > 0)
            {
                foreach (var cashFlow in floatingLeg)
                {
                    cashFlow.registerWith(update);
                }
            }

            legs_[0] = cpiLeg;
            legs_[1] = floatingLeg;

            if (type_ == Type.Payer)
            {
                payer_[0] = 1.0;
                payer_[1] = -1.0;
            }
            else
            {
                payer_[0] = -1.0;
                payer_[1] = 1.0;
            }
        }

        public virtual double baseCPI() => baseCPI_;

        // legs
        public virtual List<CashFlow> cpiLeg() => legs_[0];

        public virtual double fairRate()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(fairRate_ != null, () => "result not available");
            return fairRate_.GetValueOrDefault();
        }

        public virtual double fairSpread()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(fairSpread_ != null, () => "result not available");
            return fairSpread_.GetValueOrDefault();
        }

        // other
        public override void fetchResults(IPricingEngineResults r)
        {
            // copy from VanillaSwap
            // works because similarly simple instrument
            // that we always expect to be priced with a swap engine

            base.fetchResults(r);

            if (r is Results results)
            {
                // might be a swap engine, so no error is thrown
                fairRate_ = results.fairRate;
                fairSpread_ = results.fairSpread;
            }
            else
            {
                fairRate_ = null;
                fairSpread_ = null;
            }

            if (fairRate_ == null)
            {
                // calculate it from other results
                if (legBPS_[0] != null)
                {
                    fairRate_ = fixedRate_ - NPV_ / (legBPS_[0] / Const.BASIS_POINT);
                }
            }

            if (fairSpread_ == null)
            {
                // ditto
                if (legBPS_[1] != null)
                {
                    fairSpread_ = spread_ - NPV_ / (legBPS_[1] / Const.BASIS_POINT);
                }
            }
        }

        public virtual DayCounter fixedDayCount() => fixedDayCount_;

        public virtual ZeroInflationIndex fixedIndex() => fixedIndex_;

        // fixed rate x inflation
        public virtual double fixedLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[0] != null, () => "result not available");
            return legNPV_[0].GetValueOrDefault();
        }

        public virtual BusinessDayConvention fixedPaymentRoll() => fixedPaymentRoll_;

        // fixed rate x inflation
        public virtual double fixedRate() => fixedRate_;

        public virtual Schedule fixedSchedule() => fixedSchedule_;

        public virtual int fixingDays() => fixingDays_;

        public virtual DayCounter floatDayCount() => floatDayCount_;

        public virtual IborIndex floatIndex() => floatIndex_;

        public virtual List<CashFlow> floatLeg() => legs_[1];

        // results
        // float+spread
        public virtual double floatLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[1] != null, () => "result not available");
            return legNPV_[1].GetValueOrDefault();
        }

        public virtual BusinessDayConvention floatPaymentRoll() => floatPaymentRoll_;

        public virtual Schedule floatSchedule() => floatSchedule_;

        public virtual double inflationNominal() => inflationNominal_;

        public virtual double nominal() => nominal_;

        public virtual InterpolationType observationInterpolation() => observationInterpolation_;

        public virtual Period observationLag() => observationLag_;

        // float+spread
        public virtual double spread() => spread_;

        public virtual bool subtractInflationNominal() => subtractInflationNominal_;

        // inspectors
        public virtual Type type() => type_;

        protected override void setupExpired()
        {
            base.setupExpired();
            legBPS_[0] = legBPS_[1] = 0.0;
            fairRate_ = null;
            fairSpread_ = null;
        }
    }
}
