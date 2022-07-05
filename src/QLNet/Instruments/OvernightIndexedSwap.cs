/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 *             2017               Jean-Camille Tournier (tournier.jc@openmailbox.org)
 *
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
    //! Overnight indexed swap: fix vs compounded overnight rate
    [PublicAPI]
    public class OvernightIndexedSwap : Swap
    {
        public enum Type
        {
            Receiver = -1,
            Payer = 1
        }

        private DayCounter fixedDC_;
        private double fixedNominal_;
        private Frequency fixedPaymentFrequency_;
        private double fixedRate_;
        private OvernightIndex overnightIndex_;
        private double overnightNominal_;
        private Frequency overnightPaymentFrequency_;
        private double spread_;
        private Type type_;

        public OvernightIndexedSwap(Type type,
            double nominal,
            Schedule schedule,
            double fixedRate,
            DayCounter fixedDC,
            OvernightIndex overnightIndex,
            double spread) :
            base(2)
        {
            type_ = type;
            fixedNominal_ = nominal;
            overnightNominal_ = nominal;
            fixedPaymentFrequency_ = schedule.tenor().frequency();
            overnightPaymentFrequency_ = schedule.tenor().frequency();
            fixedRate_ = fixedRate;
            fixedDC_ = fixedDC;
            overnightIndex_ = overnightIndex;
            spread_ = spread;

            if (fixedDC_ == null)
            {
                fixedDC_ = overnightIndex_.dayCounter();
            }

            legs_[0] = new FixedRateLeg(schedule)
                .withCouponRates(fixedRate_, fixedDC_)
                .withNotionals(nominal);

            legs_[1] = new OvernightLeg(schedule, overnightIndex_)
                .withNotionals(nominal)
                .withSpreads(spread_);

            for (var j = 0; j < 2; ++j)
            {
                for (var i = 0; i < legs_[j].Count; i++)
                {
                    legs_[j][i].registerWith(update);
                }
            }

            switch (type_)
            {
                case Type.Payer:
                    payer_[0] = -1.0;
                    payer_[1] = +1.0;
                    break;
                case Type.Receiver:
                    payer_[0] = +1.0;
                    payer_[1] = -1.0;
                    break;
                default:
                    Utils.QL_FAIL("Unknown overnight-swap ExerciseType");
                    break;
            }
        }

        public OvernightIndexedSwap(Type type,
            double fixedNominal,
            Schedule fixedSchedule,
            double fixedRate,
            DayCounter fixedDC,
            double overnightNominal,
            Schedule overnightSchedule,
            OvernightIndex overnightIndex,
            double spread) :
            base(2)
        {
            type_ = type;
            fixedNominal_ = fixedNominal;
            overnightNominal_ = overnightNominal;
            fixedPaymentFrequency_ = fixedSchedule.tenor().frequency();
            overnightPaymentFrequency_ = overnightSchedule.tenor().frequency();
            fixedRate_ = fixedRate;
            fixedDC_ = fixedDC;
            overnightIndex_ = overnightIndex;
            spread_ = spread;

            if (fixedDC_ == null)
            {
                fixedDC_ = overnightIndex_.dayCounter();
            }

            legs_[0] = new FixedRateLeg(fixedSchedule)
                .withCouponRates(fixedRate_, fixedDC_)
                .withNotionals(fixedNominal_);

            legs_[1] = new OvernightLeg(overnightSchedule, overnightIndex_)
                .withNotionals(overnightNominal_)
                .withSpreads(spread_);

            for (var j = 0; j < 2; ++j)
            {
                for (var i = 0; i < legs_[j].Count; i++)
                {
                    legs_[j][i].registerWith(update);
                }
            }

            switch (type_)
            {
                case Type.Payer:
                    payer_[0] = -1.0;
                    payer_[1] = +1.0;
                    break;
                case Type.Receiver:
                    payer_[0] = +1.0;
                    payer_[1] = -1.0;
                    break;
                default:
                    Utils.QL_FAIL("Unknown overnight-swap ExerciseType");
                    break;
            }
        }

        public double? fairRate()
        {
            calculate();
            return fixedRate_ - NPV_ / (fixedLegBPS() / Const.BASIS_POINT);
        }

        public double? fairSpread()
        {
            calculate();
            return spread_ - NPV_ / (overnightLegBPS() / Const.BASIS_POINT);
        }

        public DayCounter fixedDayCount() => fixedDC_;

        public List<CashFlow> fixedLeg() => legs_[0];

        public double? fixedLegBPS()
        {
            calculate();
            Utils.QL_REQUIRE(legBPS_[0] != null, () => "result not available");
            return legBPS_[0];
        }

        public double? fixedLegNPV()
        {
            calculate();
            Utils.QL_REQUIRE(legNPV_[0] != null, () => "result not available");
            return legNPV_[0];
        }

        public double fixedNominal() => fixedNominal_;

        public Frequency fixedPaymentFrequency() => fixedPaymentFrequency_;

        public double fixedRate() => fixedRate_;

        public List<CashFlow> overnightLeg() => legs_[1];

        public double? overnightLegBPS()
        {
            calculate();
            Utils.QL_REQUIRE(legBPS_[1] != null, () => "result not available");
            return legBPS_[1];
        }

        public double? overnightLegNPV()
        {
            calculate();
            Utils.QL_REQUIRE(legNPV_[1] != null, () => "result not available");
            return legNPV_[1];
        }

        public double overnightNominal() => overnightNominal_;

        public Frequency overnightPaymentFrequency() => overnightPaymentFrequency_;

        public double spread() => spread_;

        public Type type() => type_;
    }
}
