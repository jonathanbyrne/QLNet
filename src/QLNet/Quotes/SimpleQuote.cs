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

using System;
using JetBrains.Annotations;
using QLNet.Extensions;

namespace QLNet.Quotes
{
    // simple quote class
    //! market element returning a stored value
    [PublicAPI]
    public class SimpleQuote : Quote
    {
        private double? value_;

        public SimpleQuote()
        {
        }

        public SimpleQuote(double? value)
        {
            value_ = value;
        }

        public override bool isValid() => value_ != null;

        public void reset()
        {
            setValue(null);
        }

        //! returns the difference between the new value and the old value
        public double setValue(double? value)
        {
            var diff = value - value_;
            if (diff.IsNotEqual(0.0))
            {
                value_ = value;
                notifyObservers();
            }

            return diff.GetValueOrDefault();
        }

        //! Quote interface
        public override double value()
        {
            if (!isValid())
            {
                throw new ArgumentException("invalid SimpleQuote");
            }

            return value_.GetValueOrDefault();
        }
    }
}
