//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Indexes;
using QLNet.Math;
using QLNet.Termstructures;
using QLNet.Time;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.Termstructures.Inflation
{
    //! Provides cpi cap/floor prices by interpolation and put/call parity (not cap/floor/swap* parity).
    /*!
        The inflation index MUST contain a ZeroInflationTermStructure as
        this is used to create ATM.  Unlike YoY price surfaces we
        assume that 1) an ATM ZeroInflationTermStructure is available
        and 2) that it is safe to use it.  This is supported by the
        fact that no stripping is required for CPI cap/floors as they
        only give one flow.

        cpi cap/floors have a single (one) flow (unlike nominal
        caps) because they observe cumulative inflation up to
        their maturity.  Options are on CPI(T)/CPI(0) but strikes
        are quoted for yearly average inflation, so require transformation
        via (1+quote)^T to obtain actual strikes.  These are consistent
        with ZCIIS quoting conventions.

        The observationLag is that for the referenced instrument prices.
        Strikes are as-quoted not as-used.
    */
    public abstract class CPICapFloorTermPriceSurface : InflationTermStructure
    {
        protected CPICapFloorTermPriceSurface(double nominal,
                                              double baseRate,  // avoids an uncontrolled crash if index has no TS
                                              Period observationLag,
                                              Calendar cal, // calendar in index may not be useful
                                              BusinessDayConvention bdc,
                                              DayCounter dc,
                                              Handle<ZeroInflationIndex> zii,
                                              Handle<YieldTermStructure> yts,
                                              List<double> cStrikes,
                                              List<double> fStrikes,
                                              List<Period> cfMaturities,
                                              Matrix cPrice,
                                              Matrix fPrice)
           : base(0, cal, baseRate, observationLag, zii.link.frequency(), zii.link.interpolated(), yts, dc)
        {
            zii_ = zii;
            cStrikes_ = cStrikes;
            fStrikes_ = fStrikes;
            cfMaturities_ = cfMaturities;
            cPrice_ = cPrice;
            fPrice_ = fPrice;
            nominal_ = nominal;
            bdc_ = bdc;

            // does the index have a TS?
            Utils.QL_REQUIRE(!zii_.link.zeroInflationTermStructure().empty(), () => "ZITS missing from index");
            Utils.QL_REQUIRE(!nominalTermStructure().empty(), () => "nominal TS missing");

            // data consistency checking, enough data?
            Utils.QL_REQUIRE(fStrikes_.Count > 1, () => "not enough floor strikes");
            Utils.QL_REQUIRE(cStrikes_.Count > 1, () => "not enough cap strikes");
            Utils.QL_REQUIRE(cfMaturities_.Count > 1, () => "not enough maturities");
            Utils.QL_REQUIRE(fStrikes_.Count == fPrice.rows(), () => "floor strikes vs floor price rows not equal");
            Utils.QL_REQUIRE(cStrikes_.Count == cPrice.rows(), () => "cap strikes vs cap price rows not equal");
            Utils.QL_REQUIRE(cfMaturities_.Count == fPrice.columns(), () => "maturities vs floor price columns not equal");
            Utils.QL_REQUIRE(cfMaturities_.Count == cPrice.columns(), () => "maturities vs cap price columns not equal");

            // data has correct properties (positive, monotonic)?
            for (var j = 0; j < cfMaturities_.Count; j++)
            {
                Utils.QL_REQUIRE(cfMaturities[j] > new Period(0, TimeUnit.Days), () => "non-positive maturities");
                if (j > 0)
                {
                    Utils.QL_REQUIRE(cfMaturities[j] > cfMaturities[j - 1], () => "non-increasing maturities");
                }
                for (var i = 0; i < fPrice_.rows(); i++)
                {
                    Utils.QL_REQUIRE(fPrice_[i, j] > 0.0, () => "non-positive floor price: " + fPrice_[i, j]);
                    if (i > 0)
                    {
                        Utils.QL_REQUIRE(fPrice_[i, j] >= fPrice_[i - 1, j], () => "non-increasing floor prices");
                    }
                }
                for (var i = 0; i < cPrice_.rows(); i++)
                {
                    Utils.QL_REQUIRE(cPrice_[i, j] > 0.0, () => "non-positive cap price: " + cPrice_[i, j]);
                    if (i > 0)
                    {
                        Utils.QL_REQUIRE(cPrice_[i, j] <= cPrice_[i - 1, j], () => "non-decreasing cap prices: "
                                         + cPrice_[i, j] + " then " + cPrice_[i - 1, j]);
                    }
                }
            }


            // Get the set of strikes, noting that repeats, overlaps are
            // expected between caps and floors but that no overlap in the
            // output is allowed so no repeats or overlaps are used
            cfStrikes_ = new List<double>();
            for (var i = 0; i < fStrikes_.Count; i++)
                cfStrikes_.Add(fStrikes[i]);
            var eps = 0.0000001;
            var maxFstrike = fStrikes_.Last();
            for (var i = 0; i < cStrikes_.Count; i++)
            {
                var k = cStrikes[i];
                if (k > maxFstrike + eps)
                    cfStrikes_.Add(k);
            }

            // final consistency checking
            Utils.QL_REQUIRE(cfStrikes_.Count > 2, () => "overall not enough strikes");
            for (var i = 1; i < cfStrikes_.Count; i++)
                Utils.QL_REQUIRE(cfStrikes_[i] > cfStrikes_[i - 1], () => "cfStrikes not increasing");

        }
        // InflationTermStructure interface
        public override Period observationLag() => zeroInflationIndex().link.zeroInflationTermStructure().link.observationLag();

        public override Date baseDate() => zeroInflationIndex().link.zeroInflationTermStructure().link.baseDate();

        //! is based on
        public Handle<ZeroInflationIndex> zeroInflationIndex() => zii_;

        //! inspectors
        /*! \note you don't know if price() is a cap or a floor
                 without checking the ZeroInflation ATM level.
        */
        public virtual double nominal() => nominal_;

        public virtual BusinessDayConvention businessDayConvention() => bdc_;

        //! \warning you MUST remind the compiler in any descendants with the using:: mechanism
        //!          because you overload the names
        //! remember that the strikes use the quoting convention
        public virtual double price(Period d, double k) => price(cpiOptionDateFromTenor(d), k);

        public virtual double capPrice(Period d, double k) => capPrice(cpiOptionDateFromTenor(d), k);

        public virtual double floorPrice(Period d, double k) => floorPrice(cpiOptionDateFromTenor(d), k);

        public abstract double price(Date d, double k);
        public abstract double capPrice(Date d, double k);
        public abstract double floorPrice(Date d, double k);

        public virtual List<double> strikes() => cfStrikes_;

        public virtual List<double> capStrikes() => cStrikes_;

        public virtual List<double> floorStrikes() => fStrikes_;

        public virtual List<Period> maturities() => cfMaturities_;

        public virtual Matrix capPrices() => cPrice_;

        public virtual Matrix floorPrices() => fPrice_;

        public virtual double minStrike() => cfStrikes_.First();

        public virtual double maxStrike() => cfStrikes_.Last();

        public virtual Date minDate() => referenceDate() + cfMaturities_.First(); // \TODO deal with index interpolation
        public override Date maxDate() => referenceDate() + cfMaturities_.Last();

        public virtual Date cpiOptionDateFromTenor(Period p) => new Date(calendar().adjust(referenceDate() + p, businessDayConvention()));

        protected virtual bool checkStrike(double K) => minStrike() <= K && K <= maxStrike();

        protected virtual bool checkMaturity(Date d) => minDate() <= d && d <= maxDate();

        protected Handle<ZeroInflationIndex> zii_;
        // data
        protected List<double> cStrikes_;
        protected List<double> fStrikes_;
        protected List<Period> cfMaturities_;
        protected List<double> cfMaturityTimes_;
        protected Matrix cPrice_;
        protected Matrix fPrice_;
        // constructed
        protected List<double> cfStrikes_;

        private double nominal_;
        private BusinessDayConvention bdc_;
    }
}
