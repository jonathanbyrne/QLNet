﻿/*
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

using JetBrains.Annotations;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class PSACurve : IPrepayModel
    {
        private double _multi;
        private Date _startDate;

        public PSACurve(Date startdate)
            : this(startdate, 1)
        {
        }

        public PSACurve(Date startdate, double multiplier)
        {
            _startDate = startdate;
            _multi = multiplier;
        }

        public double getCPR(Date valDate)
        {
            var dayCounter = new Thirty360();
            var d = dayCounter.dayCount(_startDate, valDate) / 30 + 1;

            return (d <= 30 ? 0.06 * (d / 30d) : 0.06) * _multi;
        }

        public double getSMM(Date valDate) => 1 - System.Math.Pow(1 - getCPR(valDate), 1 / 12d);
    }
}
