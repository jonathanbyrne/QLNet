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

using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.montecarlo
{
    //! single-factor random walk
    /*! \ingroup mcarlo

        \note the path includes the initial asset value as its first point.
    */

    [PublicAPI]
    public class Path : IPath
    {
        private TimeGrid timeGrid_;
        private Vector values_;

        // required for generics
        public Path()
        {
        }

        public Path(TimeGrid timeGrid) : this(timeGrid, new Vector())
        {
        }

        public Path(TimeGrid timeGrid, Vector values)
        {
            timeGrid_ = timeGrid;
            values_ = values.Clone();
            if (values_.empty())
            {
                values_ = new Vector(timeGrid_.size());
            }

            QLNet.Utils.QL_REQUIRE(values_.size() == timeGrid_.size(), () => "different number of times and asset values");
        }

        //! asset value at the \f$ i \f$-th point
        public double this[int i]
        {
            get => values_[i];
            set => values_[i] = value;
        }

        //! final asset value
        public double back() => values_.Last();

        // ICloneable interface
        public object Clone()
        {
            var temp = (Path)MemberwiseClone();
            temp.values_ = new Vector(values_);
            return temp;
        }

        // inspectors
        public bool empty() => timeGrid_.empty();

        //! initial asset value
        public double front() => values_.First();

        public int length() => timeGrid_.size();

        public void setFront(double value)
        {
            values_[0] = value;
        }

        //! time at the \f$ i \f$-th point
        public double time(int i) => timeGrid_[i];

        //! time grid
        public TimeGrid timeGrid() => timeGrid_;

        public double value(int i) => values_[i];
    }
}
