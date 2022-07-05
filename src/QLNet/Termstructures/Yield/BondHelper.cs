/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Linq;
using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Pricingengines.Bond;
using QLNet.Quotes;

namespace QLNet.Termstructures.Yield
{
    //! Bond helper for curve bootstrap
    /*! \warning This class assumes that the reference date
                 does not change between calls of setTermStructure().
    */
    [PublicAPI]
    public class BondHelper : RateHelper
    {
        protected Bond bond_;
        protected RelinkableHandle<YieldTermStructure> termStructureHandle_;
        protected bool useCleanPrice_;

        /*! \warning Setting a pricing engine to the passed bond from
                     external code will cause the bootstrap to fail or
                     to give wrong results. It is advised to discard
                     the bond after creating the helper, so that the
                     helper has sole ownership of it.
        */
        public BondHelper(Handle<Quote> price, Bond bond, bool useCleanPrice = true)
            : base(price)
        {
            bond_ = bond;

            // the bond's last cashflow date, which can be later than
            // bond's maturity date because of adjustment
            latestDate_ = bond_.cashflows().Last().date();
            earliestDate_ = bond_.nextCashFlowDate();

            termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
            bond_.setPricingEngine(new DiscountingBondEngine(termStructureHandle_));

            useCleanPrice_ = useCleanPrice;
        }

        public Bond bond() => bond_;

        public override double impliedQuote()
        {
            Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
            // we didn't register as observers - force calculation
            bond_.recalculate();
            if (useCleanPrice_)
            {
                return bond_.cleanPrice();
            }

            return bond_.dirtyPrice();
        }

        // RateHelper interface
        public override void setTermStructure(YieldTermStructure t)
        {
            // do not set the relinkable handle as an observer - force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        public bool useCleanPrice() => useCleanPrice_;
    }

    //! Fixed-coupon bond helper for curve bootstrap

    //! CPI bond helper for curve bootstrap
}
