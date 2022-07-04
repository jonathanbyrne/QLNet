/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

using QLNet.Instruments;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Time;
using System;
using System.Collections.Generic;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    /// <summary>
    /// dividend handler for fdm method for one equity direction
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class FdmDividendHandler : IStepCondition<Vector>
    {
        public FdmDividendHandler(DividendSchedule schedule,
                                  FdmMesher mesher,
                                  Date referenceDate,
                                  DayCounter dayCounter,
                                  int equityDirection)
        {
            x_ = new Vector(mesher.layout().dim()[equityDirection]);
            mesher_ = mesher;
            equityDirection_ = equityDirection;

            dividends_ = new List<double>();
            dividendDates_ = new List<Date>();
            dividendTimes_ = new List<double>();

            foreach (var iter in schedule)
            {
                dividends_.Add(iter.amount());
                dividendDates_.Add(iter.date());
                dividendTimes_.Add(
                   dayCounter.yearFraction(referenceDate, iter.date()));
            }

            var tmp = mesher_.locations(equityDirection);
            var spacing = mesher_.layout().spacing()[equityDirection];
            for (var i = 0; i < x_.size(); ++i)
            {
                x_[i] = System.Math.Exp(tmp[i * spacing]);
            }
        }

        public void applyTo(object o, double t)
        {
            var a = (Vector)o;
            var aCopy = new Vector(a);

            var iterIndex = dividendTimes_.BinarySearch(t);

            if (iterIndex >= 0)
            {
                var dividend = dividends_[iterIndex];

                if (mesher_.layout().dim().Count == 1)
                {
                    var interp = new LinearInterpolation(x_, x_.Count, aCopy);
                    for (var k = 0; k < x_.size(); ++k)
                    {
                        a[k] = interp.value(System.Math.Max(x_[0], x_[k] - dividend), true);
                    }
                }
                else
                {
                    var tmp = new Vector(x_.size());
                    var xSpacing = mesher_.layout().spacing()[equityDirection_];

                    for (var i = 0; i < mesher_.layout().dim().Count; ++i)
                    {
                        if (i != equityDirection_)
                        {
                            var ySpacing = mesher_.layout().spacing()[i];
                            for (var j = 0; j < mesher_.layout().dim()[i]; ++j)
                            {
                                for (var k = 0; k < x_.size(); ++k)
                                {
                                    var index = j * ySpacing + k * xSpacing;
                                    tmp[k] = aCopy[index];
                                }
                                var interp = new LinearInterpolation(x_, x_.Count, tmp);
                                for (var k = 0; k < x_.size(); ++k)
                                {
                                    var index = j * ySpacing + k * xSpacing;
                                    a[index] = interp.value(
                                                  System.Math.Max(x_[0], x_[k] - dividend), true);
                                }
                            }
                        }
                    }
                }
            }
        }

        public List<double> dividendTimes() => dividendTimes_;

        public List<Date> dividendDates() => dividendDates_;

        public List<double> dividends() => dividends_;

        protected Vector x_; // grid-equity values in physical units

        protected List<double> dividendTimes_;
        protected List<Date> dividendDates_;
        protected List<double> dividends_;
        protected FdmMesher mesher_;
        protected int equityDirection_;
    }
}
