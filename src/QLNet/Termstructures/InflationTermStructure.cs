/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Termstructures.Inflation;
using QLNet.Time;

namespace QLNet.Termstructures
{
    //! Interface for inflation term structures.
    //! \ingroup inflationtermstructures
    public abstract class InflationTermStructure : TermStructure
    {
        protected double baseRate_;
        protected Frequency frequency_;
        protected bool indexIsInterpolated_;
        protected Handle<YieldTermStructure> nominalTermStructure_;
        protected Period observationLag_;
        private Seasonality seasonality_;

        protected InflationTermStructure()
        {
        }

        // Constructors
        protected InflationTermStructure(double baseRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            DayCounter dayCounter = null,
            Seasonality seasonality = null)
            : base(dayCounter)
        {
            nominalTermStructure_ = yTS;
            observationLag_ = observationLag;
            frequency_ = frequency;
            indexIsInterpolated_ = indexIsInterpolated;
            baseRate_ = baseRate;
            nominalTermStructure_.registerWith(update);
            setSeasonality(seasonality);
        }

        protected InflationTermStructure(Date referenceDate,
            double baseRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            Calendar calendar,
            DayCounter dayCounter = null,
            Seasonality seasonality = null)
            : base(referenceDate, calendar, dayCounter)
        {
            nominalTermStructure_ = yTS;
            observationLag_ = observationLag;
            frequency_ = frequency;
            indexIsInterpolated_ = indexIsInterpolated;
            baseRate_ = baseRate;
            nominalTermStructure_.registerWith(update);
            setSeasonality(seasonality);
        }

        protected InflationTermStructure(int settlementDays,
            Calendar calendar,
            double baseRate,
            Period observationLag,
            Frequency frequency,
            bool indexIsInterpolated,
            Handle<YieldTermStructure> yTS,
            DayCounter dayCounter = null,
            Seasonality seasonality = null)
            : base(settlementDays, calendar, dayCounter)
        {
            nominalTermStructure_ = yTS;
            observationLag_ = observationLag;
            frequency_ = frequency;
            indexIsInterpolated_ = indexIsInterpolated;
            baseRate_ = baseRate;
            nominalTermStructure_.registerWith(update);
            setSeasonality(seasonality);
        }

        //! minimum (base) date
        /*! Important in inflation since it starts before nominal
            reference date.  Changes depending whether index is
            interpolated or not.  When interpolated the base date
            is just observation lag before nominal.  When not
            interpolated it is the beginning of the relevant period
            (hence it is easy to create interpolated fixings from
             a not-interpolated curve because interpolation, usually,
             of fixings is forward looking).
        */
        public abstract Date baseDate();

        public virtual double baseRate() => baseRate_;

        public virtual Frequency frequency() => frequency_;

        public bool hasSeasonality() => seasonality_ != null;

        public virtual bool indexIsInterpolated() => indexIsInterpolated_;

        public virtual Handle<YieldTermStructure> nominalTermStructure() => nominalTermStructure_;

        // Inflation interface
        //! The TS observes with a lag that is usually different from the
        //! availability lag of the index.  An inflation rate is given,
        //! by default, for the maturity requested assuming this lag.
        public virtual Period observationLag() => observationLag_;

        public Seasonality seasonality() => seasonality_;

        //! Functions to set and get seasonality.
        /*! Calling setSeasonality with no arguments means unsetting
            as the default is used to choose unsetting.
        */

        public void setSeasonality(Seasonality seasonality = null)
        {
            // always reset, whether with null or new pointer
            seasonality_ = seasonality;
            if (seasonality_ != null)
            {
                QLNet.Utils.QL_REQUIRE(seasonality_.isConsistent(this),
                    () => "Seasonality inconsistent with " + "inflation term structure");
            }

            notifyObservers();
        }

        // range-checking
        protected override void checkRange(Date d, bool extrapolate)
        {
            QLNet.Utils.QL_REQUIRE(d >= baseDate(), () => "date (" + d + ") is before base date");

            QLNet.Utils.QL_REQUIRE(extrapolate || allowsExtrapolation() || d <= maxDate(), () =>
                "date (" + d + ") is past max curve date (" + maxDate() + ")");
        }

        // This next part is required for piecewise- constructors
        // because, for inflation, they need more than just the
        // instruments to build the term structure, since the rate at
        // time 0-lag is non-zero, since we deal (effectively) with
        // "forwards".
        protected virtual void setBaseRate(double r)
        {
            baseRate_ = r;
        }
    }

    //! Interface for zero inflation term structures.
    // Child classes use templates but do not want that exposed to
    // general users.

    //! Base class for year-on-year inflation term structures.
}
