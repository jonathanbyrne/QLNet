/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    /// <summary>
    ///     Callable bond base class
    ///     <remarks>
    ///         Base callable bond class for fixed and zero coupon bonds.
    ///         Defines commonalities between fixed and zero coupon callable
    ///         bonds. At present, only European and Bermudan put/call schedules
    ///         supported (no American optionality), as defined by the Callability
    ///         class.
    ///     </remarks>
    /// </summary>
    [PublicAPI]
    public class CallableBond : Bond
    {
        public new class Arguments : Bond.Arguments
        {
            public List<Date> callabilityDates { get; set; }

            //! bond full/dirty/cash prices
            public List<double> callabilityPrices { get; set; }

            public List<double> couponAmounts { get; set; }

            public List<Date> couponDates { get; set; }

            public Frequency frequency { get; set; }

            public DayCounter paymentDayCounter { get; set; }

            public CallabilitySchedule putCallSchedule { get; set; }

            public double redemption { get; set; }

            public Date redemptionDate { get; set; }

            /// <summary>
            ///     Spread to apply to the valuation.
            ///     <remarks>
            ///         This is a continuously
            ///         componded rate added to the model. Currently only applied
            ///         by the TreeCallableFixedRateBondEngine
            ///     </remarks>
            /// </summary>
            public double spread { get; set; }

            public override void validate()
            {
                QLNet.Utils.QL_REQUIRE(settlementDate != null, () => "null settlement date");
                QLNet.Utils.QL_REQUIRE(redemption >= 0.0, () => "positive redemption required: " + redemption + " not allowed");
                QLNet.Utils.QL_REQUIRE(callabilityDates.Count == callabilityPrices.Count, () => "different number of callability dates and prices");
                QLNet.Utils.QL_REQUIRE(couponDates.Count == couponAmounts.Count, () => "different number of coupon dates and amounts");
            }
        }

        /// <summary>
        ///     base class for callable fixed rate bond engine
        /// </summary>
        public new class Engine : GenericEngine<Arguments, Results>
        {
        }

        /// <summary>
        ///     results for a callable bond calculation
        /// </summary>
        public new class Results : Bond.Results
        {
            // no extra results set yet
        }

        //
        /// <summary>
        ///     helper class for Black implied volatility calculation
        /// </summary>
        protected class ImpliedVolHelper : ISolver1d
        {
            private readonly IPricingEngine engine_;
            private readonly Instrument.Results results_;
            private readonly double targetValue_;
            private readonly SimpleQuote vol_;

            public ImpliedVolHelper(CallableBond bond, double targetValue)
            {
                targetValue_ = targetValue;
                vol_ = new SimpleQuote(0.0);
                bond.blackVolQuote_.linkTo(vol_);

                QLNet.Utils.QL_REQUIRE(bond.blackEngine_ != null, () => "Must set blackEngine_ to use impliedVolatility");

                engine_ = bond.blackEngine_;
                bond.setupArguments(engine_.getArguments());
                results_ = engine_.getResults() as Instrument.Results;
            }

            public override double value(double x)
            {
                vol_.setValue(x);
                engine_.calculate(); // get the Black NPV based on vol x
                return results_.value.Value - targetValue_;
            }
        }

        /// <summary>
        ///     Helper class for option adjusted spread calculations
        /// </summary>
        protected class NpvSpreadHelper
        {
            private readonly CallableBond bond_;
            private readonly Instrument.Results results_;

            public NpvSpreadHelper(CallableBond bond)
            {
                bond_ = bond;
                results_ = bond.engine_.getResults() as Instrument.Results;
                bond.setupArguments(bond.engine_.getArguments());
            }

            public double value(double x)
            {
                var args = bond_.engine_.getArguments() as Arguments;
                // Pops the original value when function finishes
                var originalSpread = args.spread;
                args.spread = x;
                bond_.engine_.calculate();
                args.spread = originalSpread;
                return results_.value.Value;
            }
        }

        protected class OasHelper : ISolver1d
        {
            private readonly NpvSpreadHelper npvhelper_;
            private readonly double targetValue_;

            public OasHelper(NpvSpreadHelper npvhelper, double targetValue)
            {
                npvhelper_ = npvhelper;
                targetValue_ = targetValue;
            }

            public override double value(double v) => targetValue_ - npvhelper_.value(v);
        }

        //
        /// <summary>
        ///     Black fwd yield volatility quote handle to internal blackEngine_
        /// </summary>
        protected RelinkableHandle<YieldTermStructure> blackDiscountCurve_ = new RelinkableHandle<YieldTermStructure>();
        //
        /// <summary>
        ///     must be set by derived classes for impliedVolatility() to work
        /// </summary>
        protected IPricingEngine blackEngine_;
        //
        /// <summary>
        ///     Black fwd yield volatility quote handle to internal blackEngine_
        /// </summary>
        protected RelinkableHandle<Quote> blackVolQuote_ = new RelinkableHandle<Quote>();
        protected Frequency frequency_;
        protected DayCounter paymentDayCounter_;
        protected CallabilitySchedule putCallSchedule_;

        protected CallableBond(int settlementDays,
            Schedule schedule,
            DayCounter paymentDayCounter,
            Date issueDate = null,
            CallabilitySchedule putCallSchedule = null)
            : base(settlementDays, schedule.calendar(), issueDate)
        {
            paymentDayCounter_ = paymentDayCounter;
            putCallSchedule_ = putCallSchedule ?? new CallabilitySchedule();
            maturityDate_ = schedule.dates().Last();

            if (!putCallSchedule_.empty())
            {
                var finalOptionDate = Date.minDate();
                for (var i = 0; i < putCallSchedule_.Count; ++i)
                {
                    finalOptionDate = Date.Max(finalOptionDate,
                        putCallSchedule_[i].date());
                }

                QLNet.Utils.QL_REQUIRE(finalOptionDate <= maturityDate_, () => "Bond cannot mature before last call/put date");
            }

            // derived classes must set cashflows_ and frequency_
        }

        /// <summary>
        ///     Return the bond's put/call schedule
        /// </summary>
        /// <returns></returns>
        public CallabilitySchedule callability() => putCallSchedule_;

        /// <summary>
        ///     Calculate the clean price based on the given
        ///     option-adjust-spread (oas) over the given yield term
        ///     structure (engineTS)
        /// </summary>
        /// <param name="oas"></param>
        /// <param name="engineTS"></param>
        /// <param name="dayCounter"></param>
        /// <param name="compounding"></param>
        /// <param name="frequency"></param>
        /// <param name="settlement"></param>
        /// <returns></returns>
        public double cleanPriceOAS(double oas,
            Handle<YieldTermStructure> engineTS,
            DayCounter dayCounter,
            Compounding compounding,
            Frequency frequency,
            Date settlement = null)
        {
            if (settlement == null)
            {
                settlement = settlementDate();
            }

            oas = convToContinuous(oas, this, engineTS, dayCounter, compounding, frequency);

            var f = new NpvSpreadHelper(this);

            var P = f.value(oas) - accruedAmount(settlement);

            return P;
        }

        /// <summary>
        ///     Calculate the effective convexity
        ///     <remarks>
        ///         Calculate the effective convexity, i.e., the second
        ///         differential of the dirty price w.r.t. a parallel shift of
        ///         the yield term structure divided by current dirty price
        ///     </remarks>
        /// </summary>
        /// <param name="oas"></param>
        /// <param name="engineTS"></param>
        /// <param name="dayCounter"></param>
        /// <param name="compounding"></param>
        /// <param name="frequency"></param>
        /// <param name="bump"></param>
        /// <returns></returns>
        public double effectiveConvexity(double oas,
            Handle<YieldTermStructure> engineTS,
            DayCounter dayCounter,
            Compounding compounding,
            Frequency frequency,
            double bump = 2e-4)
        {
            var P = cleanPriceOAS(oas, engineTS, dayCounter, compounding, frequency);

            var Ppp = cleanPriceOAS(oas + bump, engineTS, dayCounter, compounding, frequency);

            var Pmm = cleanPriceOAS(oas - bump, engineTS, dayCounter, compounding, frequency);

            if (P.IsEqual(0.0))
            {
                return 0;
            }

            return (Ppp + Pmm - 2 * P) / (System.Math.Pow(bump, 2) * P);
        }

        /// <summary>
        ///     Calculate the effective duration
        ///     <remarks>
        ///         Calculate the effective duration, i.e., the first
        ///         differential of the dirty price w.r.t. a parallel shift of
        ///         the yield term structure divided by current dirty price
        ///     </remarks>
        /// </summary>
        /// <param name="oas"></param>
        /// <param name="engineTS"></param>
        /// <param name="dayCounter"></param>
        /// <param name="compounding"></param>
        /// <param name="frequency"></param>
        /// <param name="bump"></param>
        /// <returns></returns>
        public double effectiveDuration(double oas,
            Handle<YieldTermStructure> engineTS,
            DayCounter dayCounter,
            Compounding compounding,
            Frequency frequency,
            double bump = 2e-4)
        {
            var P = cleanPriceOAS(oas, engineTS, dayCounter, compounding, frequency);

            var Ppp = cleanPriceOAS(oas + bump, engineTS, dayCounter, compounding, frequency);

            var Pmm = cleanPriceOAS(oas - bump, engineTS, dayCounter, compounding, frequency);

            if (P.IsEqual(0.0))
            {
                return 0;
            }

            return (Pmm - Ppp) / (2 * P * bump);
        }

        /// <summary>
        ///     Returns the Black implied forward yield volatility
        ///     <remarks>
        ///         the forward yield volatility, see Hull, Fourth Edition,
        ///         Chapter 20, pg 536). Relevant only to European put/call
        ///         schedules
        ///     </remarks>
        /// </summary>
        /// <param name="targetValue"></param>
        /// <param name="discountCurve"></param>
        /// <param name="accuracy"></param>
        /// <param name="maxEvaluations"></param>
        /// <param name="minVol"></param>
        /// <param name="maxVol"></param>
        /// <returns></returns>
        public double impliedVolatility(double targetValue,
            Handle<YieldTermStructure> discountCurve,
            double accuracy,
            int maxEvaluations,
            double minVol,
            double maxVol)
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(!isExpired(), () => "instrument expired");
            var guess = 0.5 * (minVol + maxVol);
            blackDiscountCurve_.linkTo(discountCurve, false);
            var f = new ImpliedVolHelper(this, targetValue);
            var solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            return solver.solve(f, accuracy, guess, minVol, maxVol);
        }

        /// <summary>
        ///     Calculate the Option Adjusted Spread (OAS)
        ///     <remarks>
        ///         Calculates the spread that needs to be added to the the
        ///         reference curve so that the theoretical model value
        ///         matches the marketPrice.
        ///     </remarks>
        /// </summary>
        /// <param name="cleanPrice"></param>
        /// <param name="engineTS"></param>
        /// <param name="dayCounter"></param>
        /// <param name="compounding"></param>
        /// <param name="frequency"></param>
        /// <param name="settlement"></param>
        /// <param name="accuracy"></param>
        /// <param name="maxIterations"></param>
        /// <param name="guess"></param>
        /// <returns></returns>
        public double OAS(double cleanPrice,
            Handle<YieldTermStructure> engineTS,
            DayCounter dayCounter,
            Compounding compounding,
            Frequency frequency,
            Date settlement = null,
            double accuracy = 1.0e-10,
            int maxIterations = 100,
            double guess = 0.0)
        {
            if (settlement == null)
            {
                settlement = settlementDate();
            }

            var dirtyPrice = cleanPrice + accruedAmount(settlement);

            var f = new NpvSpreadHelper(this);
            var obj = new OasHelper(f, dirtyPrice);

            var solver = new Brent();
            solver.setMaxEvaluations(maxIterations);

            var step = 0.001;
            var oas = solver.solve(obj, accuracy, guess, step);

            return continuousToConv(oas,
                this,
                engineTS,
                dayCounter,
                compounding,
                frequency);
        }

        /// <summary>
        ///     Convert a continuous spread to a conventional spread to a
        ///     reference yield curve
        /// </summary>
        /// <param name="oas"></param>
        /// <param name="b"></param>
        /// <param name="yts"></param>
        /// <param name="dayCounter"></param>
        /// <param name="compounding"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private double continuousToConv(double oas,
            Bond b,
            Handle<YieldTermStructure> yts,
            DayCounter dayCounter,
            Compounding compounding,
            Frequency frequency)
        {
            var zz = yts.link.zeroRate(b.maturityDate(), dayCounter, Compounding.Continuous, Frequency.NoFrequency).value();

            var baseRate = new InterestRate(zz, dayCounter, Compounding.Continuous, Frequency.NoFrequency);

            var spreadedRate = new InterestRate(oas + zz, dayCounter, Compounding.Continuous, Frequency.NoFrequency);

            var br = baseRate.equivalentRate(dayCounter, compounding, frequency, yts.link.referenceDate(), b.maturityDate()).rate();

            var sr = spreadedRate.equivalentRate(dayCounter, compounding, frequency, yts.link.referenceDate(), b.maturityDate()).rate();

            // Return the spread
            return sr - br;
        }

        /// <summary>
        ///     Convert a conventional spread to a reference yield curve to a
        ///     continuous spread
        /// </summary>
        /// <param name="oas"></param>
        /// <param name="b"></param>
        /// <param name="yts"></param>
        /// <param name="dayCounter"></param>
        /// <param name="compounding"></param>
        /// <param name="frequency"></param>
        /// <returns></returns>
        private double convToContinuous(double oas,
            Bond b,
            Handle<YieldTermStructure> yts,
            DayCounter dayCounter,
            Compounding compounding,
            Frequency frequency)
        {
            var zz = yts.link.zeroRate(b.maturityDate(), dayCounter, compounding, frequency).value();

            var baseRate = new InterestRate(zz, dayCounter, compounding, frequency);

            var spreadedRate = new InterestRate(oas + zz, dayCounter, compounding, frequency);

            var br = baseRate.equivalentRate(dayCounter, Compounding.Continuous, Frequency.NoFrequency, yts.link.referenceDate(), b.maturityDate()).rate();

            var sr = spreadedRate.equivalentRate(dayCounter, Compounding.Continuous, Frequency.NoFrequency, yts.link.referenceDate(), b.maturityDate()).rate();

            // Return the spread
            return sr - br;
        }
    }
}
