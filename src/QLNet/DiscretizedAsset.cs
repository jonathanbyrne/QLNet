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

using System.Collections.Generic;
using QLNet.Math;

namespace QLNet
{
    //! Discretized asset class used by numerical
    public abstract class DiscretizedAsset
    {
        protected double latestPreAdjustment_, latestPostAdjustment_;
        protected double time_;
        protected Vector values_;
        private Lattice method_;

        protected DiscretizedAsset()
        {
            latestPreAdjustment_ = double.MaxValue;
            latestPostAdjustment_ = double.MaxValue;
        }

        /*! This method returns the times at which the numerical
            method should stop while rolling back the asset. Typical
            examples include payment times, exercise times and such.
  
            \note The returned values are not guaranteed to be sorted.
        */
        public abstract List<double> mandatoryTimes();

        /* Low-level interface
  
            These methods (that developers should override when
            deriving from DiscretizedAsset) are to be used by
            numerical methods and not directly by users, with the
            exception of adjustValues(), preAdjustValues() and
            postAdjustValues() that can be used together with
            partialRollback().
        */

        /*! This method should initialize the asset values to an Array
            of the given size and with values depending on the
            particular asset.
        */
        public abstract void reset(int size);

        /*! This method performs both pre- and post-adjustment */

        public void adjustValues()
        {
            preAdjustValues();
            postAdjustValues();
        }

        /* High-level interface
  
                    Users of discretized assets should use these methods in
                    order to initialize, evolve and take the present value of
                    the assets.  They call the corresponding methods in the
                    Lattice interface, to which we refer for
                    documentation.
  
                */

        public void initialize(Lattice method, double t)
        {
            method_ = method;
            method_.initialize(this, t);
        }

        public Lattice method() => method_;

        public void partialRollback(double to)
        {
            method_.partialRollback(this, to);
        }

        /*! This method will be invoked after rollback and after any
        other asset had their chance to look at the values. For
        instance, payments happening at the present time (and therefore
        not included in an option to be exercised at this time) will be
        added here.
  
        This method is not virtual; derived classes must override
        the protected postAdjustValuesImpl() method instead. */

        public void postAdjustValues()
        {
            if (!Utils.close(time(), latestPostAdjustment_))
            {
                postAdjustValuesImpl();
                latestPostAdjustment_ = time();
            }
        }

        /*! This method will be invoked after rollback and before any
        other asset (i.e., an option on this one) has any chance to
        look at the values. For instance, payments happening at times
        already spanned by the rollback will be added here.
  
        This method is not virtual; derived classes must override
        the protected preAdjustValuesImpl() method instead. */

        public void preAdjustValues()
        {
            if (!Utils.close(time(), latestPreAdjustment_))
            {
                preAdjustValuesImpl();
                latestPreAdjustment_ = time();
            }
        }

        public double presentValue() => method_.presentValue(this);

        public void rollback(double to)
        {
            method_.rollback(this, to);
        }

        // safe version of QL double* time()
        public void setTime(double t)
        {
            time_ = t;
        }

        // safe version of QL Vector* values()
        public void setValues(Vector v)
        {
            values_ = v;
        }

        public double time() => time_;

        public Vector values() => values_;

        /*! This method checks whether the asset was rolled at the given time. */

        protected bool isOnTime(double t)
        {
            var grid = method().timeGrid();
            return Utils.close(grid[grid.index(t)], time());
        }

        /*! This method performs the actual post-adjustment */

        protected virtual void postAdjustValuesImpl()
        {
        }

        /*! This method performs the actual pre-adjustment */

        protected virtual void preAdjustValuesImpl()
        {
        }
    }

    //! Useful discretized discount bond asset

    //! Discretized option on a given asset
    /*! \warning it is advised that derived classes take care of
                 creating and initializing themselves an instance of
                 the underlying.
    */
}
