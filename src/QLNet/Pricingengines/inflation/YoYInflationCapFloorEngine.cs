/*
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Termstructures.Volatility.Inflation;
using QLNet.Time;

namespace QLNet.PricingEngines.inflation
{
    //! Base YoY inflation cap/floor engine
    /*! This class doesn't know yet what sort of vol it is.  The
        inflation index must be linked to a yoy inflation term
        structure.  This provides the curves, hence the call uses a
        shared_ptr<> not a handle<> to the index.

        \ingroup inflationcapfloorengines
    */

    [PublicAPI]
    public class YoYInflationCapFloorEngine : YoYInflationCapFloor.Engine
    {
        protected YoYInflationIndex index_;
        protected Handle<YoYOptionletVolatilitySurface> volatility_;

        public YoYInflationCapFloorEngine(YoYInflationIndex index, Handle<YoYOptionletVolatilitySurface> vol)
        {
            index_ = index;
            volatility_ = vol;

            index_.registerWith(update);
            volatility_.registerWith(update);
        }

        public override void calculate()
        {
            // copy black version then adapt to others

            var value = 0.0;
            var optionlets = arguments_.startDates.Count;
            List<double> values = new InitializedList<double>(optionlets, 0.0);
            List<double> stdDevs = new InitializedList<double>(optionlets, 0.0);
            List<double> forwards = new InitializedList<double>(optionlets, 0.0);
            var type = arguments_.type;

            var yoyTS
                = index().yoyInflationTermStructure();
            var nominalTS
                = yoyTS.link.nominalTermStructure();
            var settlement = nominalTS.link.referenceDate();

            for (var i = 0; i < optionlets; ++i)
            {
                var paymentDate = arguments_.payDates[i];
                if (paymentDate > settlement)
                {
                    // discard expired caplets
                    var d = arguments_.nominals[i] *
                            arguments_.gearings[i] *
                            nominalTS.link.discount(paymentDate) *
                            arguments_.accrualTimes[i];

                    // We explicitly have the index and assume that
                    // the fixing is natural, i.e. no convexity adjustment.
                    // If that was required then we would also need
                    // nominal vols in the pricing engine, i.e. a different engine.
                    // This also means that we do not need the coupon to have
                    // a pricing engine to return the swaplet rate and then
                    // the adjusted fixing in the instrument.
                    forwards[i] = yoyTS.link.yoyRate(arguments_.fixingDates[i], new Period(0, TimeUnit.Days));
                    var forward = forwards[i];

                    var fixingDate = arguments_.fixingDates[i];
                    var sqrtTime = 0.0;
                    if (fixingDate > volatility_.link.baseDate())
                    {
                        sqrtTime = System.Math.Sqrt(volatility_.link.timeFromBase(fixingDate));
                    }

                    if (type == CapFloorType.Cap || type == CapFloorType.Collar)
                    {
                        var strike = arguments_.capRates[i].Value;
                        if (sqrtTime > 0.0)
                        {
                            stdDevs[i] = System.Math.Sqrt(volatility_.link.totalVariance(fixingDate, strike, new Period(0, TimeUnit.Days)));
                        }

                        // sttDev=0 for already-fixed dates so everything on forward
                        values[i] = optionletImpl(QLNet.Option.Type.Call, strike, forward, stdDevs[i], d);
                    }

                    if (type == CapFloorType.Floor || type == CapFloorType.Collar)
                    {
                        var strike = arguments_.floorRates[i].Value;
                        if (sqrtTime > 0.0)
                        {
                            stdDevs[i] = System.Math.Sqrt(volatility_.link.totalVariance(fixingDate, strike, new Period(0, TimeUnit.Days)));
                        }

                        var floorlet = optionletImpl(QLNet.Option.Type.Put, strike, forward, stdDevs[i], d);
                        if (type == CapFloorType.Floor)
                        {
                            values[i] = floorlet;
                        }
                        else
                        {
                            // a collar is long a cap and short a floor
                            values[i] -= floorlet;
                        }
                    }

                    value += values[i];
                }
            }

            results_.value = value;

            results_.additionalResults["optionletsPrice"] = values;
            results_.additionalResults["optionletsAtmForward"] = forwards;
            if (type != CapFloorType.Collar)
            {
                results_.additionalResults["optionletsStdDev"] = stdDevs;
            }
        }

        public YoYInflationIndex index() => index_;

        public void setVolatility(Handle<YoYOptionletVolatilitySurface> vol)
        {
            if (!volatility_.empty())
            {
                volatility_.unregisterWith(update);
            }

            volatility_ = vol;
            volatility_.registerWith(update);
            update();
        }

        public Handle<YoYOptionletVolatilitySurface> volatility() => volatility_;

        //! descendents only need to implement this
        protected virtual double optionletImpl(QLNet.Option.Type type, double strike, double forward, double stdDev,
            double d) =>
            throw new NotImplementedException("not implemented");
    }

    //! Black-formula inflation cap/floor engine (standalone, i.e. no coupon pricer)

    //! Unit Displaced Black-formula inflation cap/floor engine (standalone, i.e. no coupon pricer)

    //! Unit Displaced Black-formula inflation cap/floor engine (standalone, i.e. no coupon pricer)
}
