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
using QLNet.Math.matrixutilities;
using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.Utilities;
using QLNet.processes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;

namespace QLNet.Methods.Finitedifferences.Operators
{
    [PublicAPI]
    public class FdmBlackScholesOp : FdmLinearOpComposite
    {
        protected int direction_;
        protected FirstDerivativeOp dxMap_;
        protected TripleBandLinearOp dxxMap_;
        protected double? illegalLocalVolOverwrite_;
        protected LocalVolTermStructure localVol_;
        protected TripleBandLinearOp mapT_;
        protected FdmMesher mesher_;
        protected FdmQuantoHelper quantoHelper_;
        protected YieldTermStructure rTS_, qTS_;
        protected double strike_;
        protected BlackVolTermStructure volTS_;
        protected Vector x_;

        public FdmBlackScholesOp(FdmMesher mesher,
            GeneralizedBlackScholesProcess bsProcess,
            double strike,
            bool localVol = false,
            double? illegalLocalVolOverwrite = null,
            int direction = 0,
            FdmQuantoHelper quantoHelper = null)
        {
            mesher_ = mesher;
            rTS_ = bsProcess.riskFreeRate().currentLink();
            qTS_ = bsProcess.dividendYield().currentLink();
            volTS_ = bsProcess.blackVolatility().currentLink();
            localVol_ = localVol
                ? bsProcess.localVolatility().currentLink()
                : null;
            x_ = localVol ? new Vector(Vector.Exp(mesher.locations(direction))) : null;
            dxMap_ = new FirstDerivativeOp(direction, mesher);
            dxxMap_ = new SecondDerivativeOp(direction, mesher);
            mapT_ = new TripleBandLinearOp(direction, mesher);
            strike_ = strike;
            illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;
            direction_ = direction;
            quantoHelper_ = quantoHelper;
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

        public override Vector preconditioner(Vector r, double dt) => solve_splitting(direction_, r, dt);

        //! Time \f$t1 <= t2\f$ is required
        public override void setTime(double t1, double t2)
        {
            var r = rTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
            var q = qTS_.forwardRate(t1, t2, Compounding.Continuous).rate();

            if (localVol_ != null)
            {
                var layout = mesher_.layout();
                var endIter = layout.end();

                var v = new Vector(layout.size());
                for (var iter = layout.begin();
                     iter != endIter;
                     ++iter)
                {
                    var i = iter.index();

                    if (illegalLocalVolOverwrite_ == null)
                    {
                        var t = localVol_.localVol(0.5 * (t1 + t2), x_[i], true);
                        v[i] = t * t;
                    }
                    else
                    {
                        try
                        {
                            var t = localVol_.localVol(0.5 * (t1 + t2), x_[i], true);
                            v[i] = t * t;
                        }
                        catch
                        {
                            v[i] = illegalLocalVolOverwrite_.Value * illegalLocalVolOverwrite_.Value;
                        }
                    }
                }

                if (quantoHelper_ != null)
                {
                    mapT_.axpyb(r - q - 0.5 * v
                                - quantoHelper_.quantoAdjustment(Vector.Sqrt(v), t1, t2),
                        dxMap_,
                        dxxMap_.mult(0.5 * v), new Vector(1, -r));
                }
                else
                {
                    mapT_.axpyb(r - q - 0.5 * v, dxMap_,
                        dxxMap_.mult(0.5 * v), new Vector(1, -r));
                }
            }
            else
            {
                var vv = volTS_.blackForwardVariance(t1, t2, strike_) / (t2 - t1);

                if (quantoHelper_ != null)
                {
                    mapT_.axpyb(new Vector(1, r - q - 0.5 * vv)
                                - quantoHelper_.quantoAdjustment(new Vector(1, System.Math.Sqrt(vv)), t1, t2),
                        dxMap_,
                        dxxMap_.mult(0.5 * new Vector(mesher_.layout().size(), vv)),
                        new Vector(1, -r));
                }
                else
                {
                    mapT_.axpyb(new Vector(1, r - q - 0.5 * vv), dxMap_,
                        dxxMap_.mult(0.5 * new Vector(mesher_.layout().size(), vv)),
                        new Vector(1, -r));
                }
            }
        }

        public override int size() => 1;

        public override Vector solve_splitting(int direction, Vector r, double dt)
        {
            if (direction == direction_)
            {
                return mapT_.solve_splitting(r, dt);
            }

            var retVal = new Vector(r);
            return retVal;
        }

        public override List<SparseMatrix> toMatrixDecomp()
        {
            List<SparseMatrix> retVal = new InitializedList<SparseMatrix>(1, mapT_.toMatrix());
            return retVal;
        }

        #region IOperator interface

        public override IOperator identity(int size) => null;

        public override Vector applyTo(Vector v) => null;

        public override Vector solveFor(Vector rhs) => null;

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
