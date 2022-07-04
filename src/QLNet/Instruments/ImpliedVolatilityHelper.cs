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

using QLNet.Math.Solvers1d;
using QLNet.processes;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using System;

namespace QLNet.Instruments
{
    //! helper class for one-asset implied-volatility calculation
    /*! The passed engine must be linked to the passed quote (see,
        e.g., VanillaOption to see how this can be achieved.) */
    public static class ImpliedVolatilityHelper
    {
        public static double calculate(Instrument instrument, IPricingEngine engine, SimpleQuote volQuote,
                                       double targetValue, double accuracy, int maxEvaluations, double minVol, double maxVol)
        {

            instrument.setupArguments(engine.getArguments());
            engine.getArguments().validate();

            var f = new PriceError(engine, volQuote, targetValue);
            var solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            var guess = (minVol + maxVol) / 2.0;
            var result = solver.solve(f, accuracy, guess, minVol, maxVol);
            return result;
        }

        public static GeneralizedBlackScholesProcess clone(GeneralizedBlackScholesProcess process, SimpleQuote volQuote)
        {
            var stateVariable = process.stateVariable();
            var dividendYield = process.dividendYield();
            var riskFreeRate = process.riskFreeRate();

            var blackVol = process.blackVolatility();
            var volatility = new Handle<BlackVolTermStructure>(new BlackConstantVol(blackVol.link.referenceDate(),
                                                                                    blackVol.link.calendar(), new Handle<Quote>(volQuote),
                                                                                    blackVol.link.dayCounter()));

            return new GeneralizedBlackScholesProcess(stateVariable, dividendYield, riskFreeRate, volatility);
        }
    }
}
