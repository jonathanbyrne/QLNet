/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 *
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

namespace QLNet.Math.Optimization
{
    //! Base class for line search
    public abstract class LineSearch
    {
        protected Vector gradient_ = new Vector();
        protected double qpt_;
        //! cost function value and gradient norm corresponding to xtd_
        protected double qt_;

        //! current values of the search direction
        protected Vector searchDirection_;
        //! flag to know if linesearch succeed
        protected bool succeed_;
        //! new x and its gradient
        protected Vector xtd_;

        //! Default constructor
        protected LineSearch() : this(0.0)
        {
        }

        protected LineSearch(double UnnamedParameter1)
        {
            qt_ = 0.0;
            qpt_ = 0.0;
            succeed_ = true;
        }

        //! current value of the search direction
        public Vector searchDirection
        {
            get => searchDirection_;
            set => searchDirection_ = value;
        }

        //! Perform line search
        public abstract double value(Problem P, ref EndCriteria.Type ecType, EndCriteria NamelessParameter3, double t_ini); // initial value of line-search step

        //! return last cost function value
        public double lastFunctionValue() => qt_;

        //! return last gradient
        public Vector lastGradient() => gradient_;

        //! return square norm of last gradient
        public double lastGradientNorm2() => qpt_;

        //! return last x value
        public Vector lastX() => xtd_;

        public bool succeed() => succeed_;

        public double update(ref Vector data, Vector direction, double beta, Constraint constraint)
        {
            var diff = beta;
            var newParams = data + diff * direction;
            var valid = constraint.test(newParams);
            var icount = 0;
            while (!valid)
            {
                QLNet.Utils.QL_REQUIRE(icount <= 200, () => "can't update linesearch");
                diff *= 0.5;
                icount++;
                newParams = data + diff * direction;
                valid = constraint.test(newParams);
            }

            data += diff * direction;
            return diff;
        }
    }
}
