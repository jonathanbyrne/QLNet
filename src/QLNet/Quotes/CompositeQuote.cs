﻿/*
 Copyright (C) 2008-2009 Andrea Maggiulli

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
using JetBrains.Annotations;

namespace QLNet.Quotes
{
    [PublicAPI]
    public class CompositeQuote : Quote
    {
        //! market element whose value depends on two other market element
        /*! \test the correctness of the returned values is tested by
                  checking them against numerical calculations.
        */
        private Handle<Quote> element1_;
        private Handle<Quote> element2_;
        private Func<double, double, double> f_;

        public CompositeQuote(Handle<Quote> element1, Handle<Quote> element2, Func<double, double, double> f)
        {
            element1_ = element1;
            element2_ = element2;
            f_ = f;

            element1_.registerWith(update);
            element2_.registerWith(update);
        }

        public override bool isValid() => element1_.link.isValid() && element2_.link.isValid();

        public void update()
        {
            notifyObservers();
        }

        //! Quote interface
        public override double value()
        {
            if (!isValid())
            {
                throw new ArgumentException("invalid DerivedQuote");
            }

            return f_(element1_.link.value(), element2_.link.value());
        }

        // inspectors
        public double value1() => element1_.link.value();

        public double value2() => element2_.link.value();
    }
}
