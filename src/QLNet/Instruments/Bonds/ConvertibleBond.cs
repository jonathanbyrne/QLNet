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
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    //! %callability leaving to the holder the possibility to convert

    //! base class for convertible bonds
    [PublicAPI]
    public class ConvertibleBond : Bond
    {
        [PublicAPI]
        public class option : OneAssetOption
        {
            public new class Arguments : Option.Arguments
            {
                public Arguments()
                {
                    conversionRatio = null;
                    settlementDays = null;
                    redemption = null;
                }

                public List<Date> callabilityDates { get; set; }

                public List<double> callabilityPrices { get; set; }

                public List<double?> callabilityTriggers { get; set; }

                public List<Callability.Type> callabilityTypes { get; set; }

                public double? conversionRatio { get; set; }

                public List<double> couponAmounts { get; set; }

                public List<Date> couponDates { get; set; }

                public Handle<Quote> creditSpread { get; set; }

                public List<Date> dividendDates { get; set; }

                public DividendSchedule dividends { get; set; }

                public Date issueDate { get; set; }

                public double? redemption { get; set; }

                public Date settlementDate { get; set; }

                public int? settlementDays { get; set; }

                public override void validate()
                {
                    base.validate();

                    Utils.QL_REQUIRE(conversionRatio != null, () => "null conversion ratio");
                    Utils.QL_REQUIRE(conversionRatio > 0.0,
                        () => "positive conversion ratio required: " + conversionRatio + " not allowed");

                    Utils.QL_REQUIRE(redemption != null, () => "null redemption");
                    Utils.QL_REQUIRE(redemption >= 0.0, () => "positive redemption required: " + redemption + " not allowed");

                    Utils.QL_REQUIRE(settlementDate != null, () => "null settlement date");

                    Utils.QL_REQUIRE(settlementDays != null, () => "null settlement days");

                    Utils.QL_REQUIRE(callabilityDates.Count == callabilityTypes.Count,
                        () => "different number of callability dates and types");
                    Utils.QL_REQUIRE(callabilityDates.Count == callabilityPrices.Count,
                        () => "different number of callability dates and prices");
                    Utils.QL_REQUIRE(callabilityDates.Count == callabilityTriggers.Count,
                        () => "different number of callability dates and triggers");

                    Utils.QL_REQUIRE(couponDates.Count == couponAmounts.Count,
                        () => "different number of coupon dates and amounts");
                }
            }

            public new class Engine : GenericEngine<Arguments,
                Results>
            {
            }

            private ConvertibleBond bond_;
            private CallabilitySchedule callability_;
            private List<CashFlow> cashflows_;
            private double conversionRatio_;
            private Handle<Quote> creditSpread_;
            private DayCounter dayCounter_;
            private DividendSchedule dividends_;
            private Date issueDate_;
            private double redemption_;
            private Schedule schedule_;
            private int settlementDays_;

            public option(ConvertibleBond bond,
                Exercise exercise,
                double conversionRatio,
                DividendSchedule dividends,
                CallabilitySchedule callability,
                Handle<Quote> creditSpread,
                List<CashFlow> cashflows,
                DayCounter dayCounter,
                Schedule schedule,
                Date issueDate,
                int settlementDays,
                double redemption)
                : base(new PlainVanillaPayoff(Type.Call, bond.notionals()[0] / 100.0 * redemption / conversionRatio),
                    exercise)
            {
                bond_ = bond;
                conversionRatio_ = conversionRatio;
                callability_ = callability;
                dividends_ = dividends;
                creditSpread_ = creditSpread;
                cashflows_ = cashflows;
                dayCounter_ = dayCounter;
                issueDate_ = issueDate;
                schedule_ = schedule;
                settlementDays_ = settlementDays;
                redemption_ = redemption;
            }

            public override void setupArguments(IPricingEngineArguments args)
            {
                base.setupArguments(args);

                var moreArgs = args as Arguments;
                Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");

                moreArgs.conversionRatio = conversionRatio_;

                var settlement = bond_.settlementDate();

                var n = callability_.Count;
                if (moreArgs.callabilityDates == null)
                {
                    moreArgs.callabilityDates = new List<Date>();
                }
                else
                {
                    moreArgs.callabilityDates.Clear();
                }

                if (moreArgs.callabilityTypes == null)
                {
                    moreArgs.callabilityTypes = new List<Callability.Type>();
                }
                else
                {
                    moreArgs.callabilityTypes.Clear();
                }

                if (moreArgs.callabilityPrices == null)
                {
                    moreArgs.callabilityPrices = new List<double>();
                }
                else
                {
                    moreArgs.callabilityPrices.Clear();
                }

                if (moreArgs.callabilityTriggers == null)
                {
                    moreArgs.callabilityTriggers = new List<double?>();
                }
                else
                {
                    moreArgs.callabilityTriggers.Clear();
                }

                for (var i = 0; i < n; i++)
                {
                    if (!callability_[i].hasOccurred(settlement, false))
                    {
                        moreArgs.callabilityTypes.Add(callability_[i].type());
                        moreArgs.callabilityDates.Add(callability_[i].date());

                        if (callability_[i].price().type() == Callability.Price.Type.Clean)
                        {
                            moreArgs.callabilityPrices.Add(callability_[i].price().amount() +
                                                           bond_.accruedAmount(callability_[i].date()));
                        }
                        else
                        {
                            moreArgs.callabilityPrices.Add(callability_[i].price().amount());
                        }

                        if (callability_[i] is SoftCallability softCall)
                        {
                            moreArgs.callabilityTriggers.Add(softCall.trigger());
                        }
                        else
                        {
                            moreArgs.callabilityTriggers.Add(null);
                        }
                    }
                }

                var cashflows = bond_.cashflows();

                if (moreArgs.couponDates == null)
                {
                    moreArgs.couponDates = new List<Date>();
                }
                else
                {
                    moreArgs.couponDates.Clear();
                }

                if (moreArgs.couponAmounts == null)
                {
                    moreArgs.couponAmounts = new List<double>();
                }
                else
                {
                    moreArgs.couponAmounts.Clear();
                }

                for (var i = 0; i < cashflows.Count - 1; i++)
                {
                    if (!cashflows[i].hasOccurred(settlement, false))
                    {
                        moreArgs.couponDates.Add(cashflows[i].date());
                        moreArgs.couponAmounts.Add(cashflows[i].amount());
                    }
                }

                if (moreArgs.dividends == null)
                {
                    moreArgs.dividends = new DividendSchedule();
                }
                else
                {
                    moreArgs.dividends.Clear();
                }

                if (moreArgs.dividendDates == null)
                {
                    moreArgs.dividendDates = new List<Date>();
                }
                else
                {
                    moreArgs.dividendDates.Clear();
                }

                for (var i = 0; i < dividends_.Count; i++)
                {
                    if (!dividends_[i].hasOccurred(settlement, false))
                    {
                        moreArgs.dividends.Add(dividends_[i]);
                        moreArgs.dividendDates.Add(dividends_[i].date());
                    }
                }

                moreArgs.creditSpread = creditSpread_;
                moreArgs.issueDate = issueDate_;
                moreArgs.settlementDate = settlement;
                moreArgs.settlementDays = settlementDays_;
                moreArgs.redemption = redemption_;
            }
        }

        protected CallabilitySchedule callability_;
        protected double conversionRatio_;
        protected Handle<Quote> creditSpread_;
        protected DividendSchedule dividends_;
        protected option option_;

        protected ConvertibleBond(Exercise exercise,
            double conversionRatio,
            DividendSchedule dividends,
            CallabilitySchedule callability,
            Handle<Quote> creditSpread,
            Date issueDate,
            int settlementDays,
            Schedule schedule,
            double redemption)
            : base(settlementDays, schedule.calendar(), issueDate)
        {
            conversionRatio_ = conversionRatio;
            callability_ = callability;
            dividends_ = dividends;
            creditSpread_ = creditSpread;

            maturityDate_ = schedule.endDate();

            if (!callability.empty())
            {
                Utils.QL_REQUIRE(callability.Last().date() <= maturityDate_, () =>
                    "last callability date ("
                    + callability.Last().date()
                    + ") later than maturity ("
                    + maturityDate_.ToShortDateString() + ")");
            }

            creditSpread.registerWith(update);
        }

        public CallabilitySchedule callability() => callability_;

        public double conversionRatio() => conversionRatio_;

        public Handle<Quote> creditSpread() => creditSpread_;

        public DividendSchedule dividends() => dividends_;

        protected override void performCalculations()
        {
            option_.setPricingEngine(engine_);
            NPV_ = settlementValue_ = option_.NPV();
            errorEstimate_ = null;
        }
    }

    //! convertible zero-coupon bond
    /*! \warning Most methods inherited from Bond (such as yield or
                the yield-based dirtyPrice and cleanPrice) refer to
                the underlying plain-vanilla bond and do not take
                convertibility and callability into account.
    */

    //! convertible fixed-coupon bond
    /*! \warning Most methods inherited from Bond (such as yield or
                 the yield-based dirtyPrice and cleanPrice) refer to
                 the underlying plain-vanilla bond and do not take
                 convertibility and callability into account.
    */

    //! convertible floating-rate bond
    /*! \warning Most methods inherited from Bond (such as yield or
                 the yield-based dirtyPrice and cleanPrice) refer to
                 the underlying plain-vanilla bond and do not take
                 convertibility and callability into account.
    */
}
