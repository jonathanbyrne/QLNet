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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.MatrixUtilities;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Models.Shortrate.Onefactormodels;

namespace QLNet.Methods.Finitedifferences.Operators
{
    [PublicAPI]
    public class FdmHullWhiteOp : FdmLinearOpComposite
    {
        protected int direction_;
        protected TripleBandLinearOp dzMap_;
        protected TripleBandLinearOp mapT_;
        protected HullWhite model_;
        protected Vector x_;

        public FdmHullWhiteOp(FdmMesher mesher,
            HullWhite model,
            int direction)
        {
            x_ = mesher.locations(direction);
            dzMap_ = new TripleBandLinearOp(new FirstDerivativeOp(direction, mesher).mult(-1.0 * x_ * model.a()).add(
                new SecondDerivativeOp(direction, mesher).mult(0.5 * model.sigma() * model.sigma()
                                                               * new Vector(mesher.layout().size(), 1.0))));
            mapT_ = new TripleBandLinearOp(direction, mesher);
            direction_ = direction;
            model_ = model;
        }

        public override Vector apply(Vector r) => mapT_.apply(r);

        public override Vector apply_direction(int direction, Vector r)
        {
            if (direction == direction_)
            {
                return mapT_.apply(r);
            }

            var retVal = new Vector(r.size(), 0.0);
            return retVal;
        }

        public override Vector apply_mixed(Vector r)
        {
            var retVal = new Vector(r.size(), 0.0);
            return retVal;
        }

        public override Vector preconditioner(Vector r, double s) => solve_splitting(direction_, r, s);

        //! Time \f$t1 <= t2\f$ is required
        public override void setTime(double t1, double t2)
        {
            var dynamics = model_.dynamics();

            var phi = 0.5 * (dynamics.shortRate(t1, 0.0)
                             + dynamics.shortRate(t2, 0.0));

            mapT_.axpyb(new Vector(), dzMap_, dzMap_, -1.0 * (x_ + phi));
        }

        public override int size() => 1;

        public override Vector solve_splitting(int direction, Vector r, double s)
        {
            if (direction == direction_)
            {
                return mapT_.solve_splitting(r, s);
            }

            var retVal = new Vector(r.size(), 0.0);
            return retVal;
        }

        public override List<SparseMatrix> toMatrixDecomp()
        {
            List<SparseMatrix> retVal = new InitializedList<SparseMatrix>(1, mapT_.toMatrix());
            return retVal;
        }

        #region IOperator interface

        public override IOperator identity(int size) => null;

        public override Vector applyTo(Vector v) => new Vector();

        public override Vector solveFor(Vector rhs) => new Vector();

        public override IOperator multiply(double a, IOperator D) => null;

        public override IOperator add
            (IOperator A, IOperator B) =>
            null;

        public override IOperator subtract(IOperator A, IOperator B) => null;

        public override bool isTimeDependent() => false;

        public override void setTime(double t)
        {
        }

        public override object Clone() => MemberwiseClone();

        #endregion
    }
}
