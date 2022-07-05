/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Pricingengines.vanilla;
using QLNet.processes;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Instruments
{
    //! Single-asset vanilla option (no barriers) with discrete dividends
    /*! \ingroup instruments */
    [PublicAPI]
    public class DividendVanillaOption : OneAssetOption
    {
        //! %Arguments for dividend vanilla option calculation
        public new class Arguments : Option.Arguments
        {
            public DividendSchedule cashFlow { get; set; }

            public override void validate()
            {
                base.validate();

                var exerciseDate = exercise.lastDate();

                for (var i = 0; i < cashFlow.Count; i++)
                {
                    Utils.QL_REQUIRE(cashFlow[i].date() <= exerciseDate, () =>
                        " dividend date (" + cashFlow[i].date() + ") is later than the exercise date (" + exerciseDate +
                        ")");
                }
            }
        }

        //! %Dividend-vanilla-option %engine base class
        public new class Engine : GenericEngine<Arguments, Results>
        {
        }

        private DividendSchedule cashFlow_;

        public DividendVanillaOption(StrikedTypePayoff payoff, Exercise exercise,
            List<Date> dividendDates, List<double> dividends)
            : base(payoff, exercise)
        {
            cashFlow_ = Utils.DividendVector(dividendDates, dividends);
        }

        /*! \warning see VanillaOption for notes on implied-volatility
                     calculation.
        */
        public double impliedVolatility(double targetValue, GeneralizedBlackScholesProcess process,
            double accuracy = 1.0e-4, int maxEvaluations = 100, double minVol = 1.0e-7, double maxVol = 4.0)
        {
            Utils.QL_REQUIRE(!isExpired(), () => "option expired");

            var volQuote = new SimpleQuote();

            var newProcess = ImpliedVolatilityHelper.clone(process, volQuote);

            // engines are built-in for the time being
            IPricingEngine engine = null;
            switch (exercise_.ExerciseType())
            {
                case Exercise.Type.European:
                    engine = new AnalyticDividendEuropeanEngine(newProcess);
                    break;
                case Exercise.Type.American:
                    engine = new FDDividendAmericanEngine(newProcess);
                    break;
                case Exercise.Type.Bermudan:
                    Utils.QL_FAIL("engine not available for Bermudan option with dividends");
                    break;
                default:
                    Utils.QL_FAIL("unknown exercise ExerciseType");
                    break;
            }

            return ImpliedVolatilityHelper.calculate(this, engine, volQuote, targetValue, accuracy,
                maxEvaluations, minVol, maxVol);
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var arguments = args as Arguments;
            Utils.QL_REQUIRE(arguments != null, () => "wrong engine ExerciseType");

            arguments.cashFlow = cashFlow_;
        }
    }
}
