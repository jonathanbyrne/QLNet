/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;

namespace QLNet.PricingEngines.swaption
{
    /*! Generic Black-style-formula swaption engine
        This is the base class for the Black and Bachelier swaption engines */

    // shifted lognormal ExerciseType engine

    // shifted lognormal ExerciseType engine

    [PublicAPI]
    public class BlackSwaptionEngine : BlackStyleSwaptionEngine<Black76Spec>
    {
        public BlackSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            double vol, DayCounter dc = null,
            double? displacement = 0.0,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
            : base(discountCurve, vol, dc, displacement, model)
        {
        }

        public BlackSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            Handle<Quote> vol, DayCounter dc = null,
            double? displacement = 0.0,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
            : base(discountCurve, vol, dc, displacement, model)
        {
        }

        public BlackSwaptionEngine(Handle<YieldTermStructure> discountCurve,
            Handle<SwaptionVolatilityStructure> vol,
            double? displacement = null,
            CashAnnuityModel model = CashAnnuityModel.DiscountCurve)
            : base(discountCurve, vol, displacement, model)
        {
            QLNet.Utils.QL_REQUIRE(vol.link.volatilityType() == VolatilityType.ShiftedLognormal,
                () => "BlackSwaptionEngine requires (shifted) lognormal input volatility");
        }
    }
}
