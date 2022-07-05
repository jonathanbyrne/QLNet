/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! swap paying Libor against BMA coupons
    [PublicAPI]
    public class BMASwap : Swap
    {
        public enum Type
        {
            Receiver = -1,
            Payer = 1
        }

        private double liborFraction_;
        private double liborSpread_;
        private double nominal_;
        private Type type_;

        public BMASwap(Type type, double nominal,
            // Libor leg
            Schedule liborSchedule, double liborFraction, double liborSpread, IborIndex liborIndex, DayCounter liborDayCount,
            // BMA leg
            Schedule bmaSchedule, BMAIndex bmaIndex, DayCounter bmaDayCount)
            : base(2)
        {
            type_ = type;
            nominal_ = nominal;
            liborFraction_ = liborFraction;
            liborSpread_ = liborSpread;

            var convention = liborSchedule.businessDayConvention();

            legs_[0] = new IborLeg(liborSchedule, liborIndex)
                .withPaymentDayCounter(liborDayCount)
                .withFixingDays(liborIndex.fixingDays())
                .withGearings(liborFraction)
                .withSpreads(liborSpread)
                .withNotionals(nominal)
                .withPaymentAdjustment(convention);

            legs_[1] = new AverageBmaLeg(bmaSchedule, bmaIndex)
                .WithPaymentDayCounter(bmaDayCount)
                .withNotionals(nominal)
                .withPaymentAdjustment(bmaSchedule.businessDayConvention());

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
                    payer_[0] = +1.0;
                    payer_[1] = -1.0;
                    break;
                case Type.Receiver:
                    payer_[0] = -1.0;
                    payer_[1] = +1.0;
                    break;
                default:
                    QLNet.Utils.QL_FAIL("Unknown BMA-swap ExerciseType");
                    break;
            }
        }

        public List<CashFlow> bmaLeg() => legs_[1];

        public double bmaLegBPS()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legBPS_[1] != null, () => "result not available");
            return legBPS_[1].GetValueOrDefault();
        }

        public double bmaLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[1] != null, () => "result not available");
            return legNPV_[1].GetValueOrDefault();
        }

        public Type BmaSwapType() => type_;

        public double fairLiborFraction()
        {
            var spreadNPV = liborSpread_ / Const.BASIS_POINT * liborLegBPS();
            var pureLiborNPV = liborLegNPV() - spreadNPV;
            QLNet.Utils.QL_REQUIRE(pureLiborNPV.IsNotEqual(0.0), () => "result not available (null libor NPV)");
            return -liborFraction_ * (bmaLegNPV() + spreadNPV) / pureLiborNPV;
        }

        public double fairLiborSpread() => liborSpread_ - NPV() / (liborLegBPS() / Const.BASIS_POINT);

        public double liborFraction() => liborFraction_;

        public List<CashFlow> liborLeg() => legs_[0];

        public double liborLegBPS()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legBPS_[0] != null, () => "result not available");
            return legBPS_[0].GetValueOrDefault();
        }

        public double liborLegNPV()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(legNPV_[0] != null, () => "result not available");
            return legNPV_[0].GetValueOrDefault();
        }

        public double liborSpread() => liborSpread_;

        public double nominal() => nominal_;
    }
}
