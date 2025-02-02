﻿/*
 Copyright (C) 2008,2009 Andrea Maggiulli

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
using System.Linq;
using QLNet.Patterns;
using QLNet.Time;

namespace QLNet.Quotes
{
    //! Quote adapter for the last fixing available of a given Index
    internal class LastFixingQuote : Quote, IObserver
    {
        protected Index index_;

        public LastFixingQuote(Index index)
        {
            index_ = index;
            index_.registerWith(update);
        }

        public override bool isValid() => index_.timeSeries().Count > 0;

        public Date referenceDate() => index_.timeSeries().Keys.Last(); // must be tested

        public void update()
        {
            notifyObservers();
        }

        //! Quote interface
        public override double value()
        {
            if (!isValid())
            {
                throw new ArgumentException(index_.name() + " has no fixing");
            }

            return index_.fixing(referenceDate());
        }
    }
}
