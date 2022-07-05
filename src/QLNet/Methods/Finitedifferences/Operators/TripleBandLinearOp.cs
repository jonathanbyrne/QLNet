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

namespace QLNet.Methods.Finitedifferences.Operators
{
    [PublicAPI]
    public class TripleBandLinearOp : FdmLinearOp
    {
        protected int direction_;
        protected List<int> i0_, i2_;
        protected List<double> lower_, diag_, upper_;
        protected FdmMesher mesher_;
        protected List<int> reverseIndex_;

        public TripleBandLinearOp(int direction, FdmMesher mesher)
        {
            direction_ = direction;
            i0_ = new InitializedList<int>(mesher.layout().size());
            i2_ = new InitializedList<int>(mesher.layout().size());
            reverseIndex_ = new InitializedList<int>(mesher.layout().size());
            lower_ = new InitializedList<double>(mesher.layout().size());
            diag_ = new InitializedList<double>(mesher.layout().size());
            upper_ = new InitializedList<double>(mesher.layout().size());
            mesher_ = mesher;

            var layout = mesher.layout();
            var endIter = layout.end();

            int tmp;
            var newDim = new List<int>(layout.dim());
            tmp = newDim[direction_];
            newDim[direction_] = newDim[0];
            newDim[0] = tmp;

            var newSpacing = new FdmLinearOpLayout(newDim).spacing();
            tmp = newSpacing[direction_];
            newSpacing[direction_] = newSpacing[0];
            newSpacing[0] = tmp;

            for (var iter = layout.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();

                i0_[i] = layout.neighbourhood(iter, direction, -1);
                i2_[i] = layout.neighbourhood(iter, direction, 1);

                var coordinates = iter.coordinates();

                var newIndex = coordinates.inner_product(0, coordinates.Count, 0, newSpacing, 0);
                reverseIndex_[newIndex] = i;
            }
        }

        public TripleBandLinearOp(TripleBandLinearOp m)
        {
            direction_ = m.direction_;
            i0_ = new InitializedList<int>(m.mesher_.layout().size());
            i2_ = new InitializedList<int>(m.mesher_.layout().size());
            reverseIndex_ = new InitializedList<int>(m.mesher_.layout().size());
            lower_ = new InitializedList<double>(m.mesher_.layout().size());
            diag_ = new InitializedList<double>(m.mesher_.layout().size());
            upper_ = new InitializedList<double>(m.mesher_.layout().size());
            mesher_ = m.mesher_;

            var len = m.mesher_.layout().size();
            m.i0_.copy(0, len, 0, i0_);
            m.i2_.copy(0, len, 0, i2_);
            m.reverseIndex_.copy(0, len, 0, reverseIndex_);
            m.lower_.copy(0, len, 0, lower_);
            m.diag_.copy(0, len, 0, diag_);
            m.upper_.copy(0, len, 0, upper_);
        }

        protected TripleBandLinearOp()
        {
        }

        public TripleBandLinearOp add
            (TripleBandLinearOp m)
        {
            var retVal = new TripleBandLinearOp(direction_, mesher_);
            var size = mesher_.layout().size();
            //#pragma omp parallel for
            for (var i = 0; i < size; ++i)
            {
                retVal.lower_[i] = lower_[i] + m.lower_[i];
                retVal.diag_[i] = diag_[i] + m.diag_[i];
                retVal.upper_[i] = upper_[i] + m.upper_[i];
            }

            return retVal;
        }

        public TripleBandLinearOp add
            (Vector u)
        {
            var retVal = new TripleBandLinearOp(direction_, mesher_);

            var size = mesher_.layout().size();
            //#pragma omp parallel for
            for (var i = 0; i < size; ++i)
            {
                retVal.lower_[i] = lower_[i];
                retVal.upper_[i] = upper_[i];
                retVal.diag_[i] = diag_[i] + u[i];
            }

            return retVal;
        }

        public override Vector apply(Vector r)
        {
            var index = mesher_.layout();

            Utils.QL_REQUIRE(r.size() == index.size(), () => "inconsistent length of r");

            var retVal = new Vector(r.size());
            //#pragma omp parallel for
            for (var i = 0; i < index.size(); ++i)
            {
                retVal[i] = r[i0_[i]] * lower_[i] + r[i] * diag_[i] + r[i2_[i]] * upper_[i];
            }

            return retVal;
        }

        public void axpyb(Vector a, TripleBandLinearOp x, TripleBandLinearOp y, Vector b)
        {
            var size = mesher_.layout().size();

            if (a.empty())
            {
                if (b.empty())
                {
                    //#pragma omp parallel for
                    for (var i = 0; i < size; ++i)
                    {
                        diag_[i] = y.diag_[i];
                        lower_[i] = y.lower_[i];
                        upper_[i] = y.upper_[i];
                    }
                }
                else
                {
                    var binc = b.size() > 1 ? 1 : 0;
                    //#pragma omp parallel for
                    for (var i = 0; i < size; ++i)
                    {
                        diag_[i] = y.diag_[i] + b[i * binc];
                        lower_[i] = y.lower_[i];
                        upper_[i] = y.upper_[i];
                    }
                }
            }
            else if (b.empty())
            {
                var ainc = a.size() > 1 ? 1 : 0;

                //#pragma omp parallel for
                for (var i = 0; i < size; ++i)
                {
                    var s = a[i * ainc];
                    diag_[i] = y.diag_[i] + s * x.diag_[i];
                    lower_[i] = y.lower_[i] + s * x.lower_[i];
                    upper_[i] = y.upper_[i] + s * x.upper_[i];
                }
            }
            else
            {
                var binc = b.size() > 1 ? 1 : 0;
                var ainc = a.size() > 1 ? 1 : 0;

                //#pragma omp parallel for
                for (var i = 0; i < size; ++i)
                {
                    var s = a[i * ainc];
                    diag_[i] = y.diag_[i] + s * x.diag_[i] + b[i * binc];
                    lower_[i] = y.lower_[i] + s * x.lower_[i];
                    upper_[i] = y.upper_[i] + s * x.upper_[i];
                }
            }
        }

        public TripleBandLinearOp mult(Vector u)
        {
            var retVal = new TripleBandLinearOp(direction_, mesher_);

            var size = mesher_.layout().size();
            //#pragma omp parallel for
            for (var i = 0; i < size; ++i)
            {
                var s = u[i];
                retVal.lower_[i] = lower_[i] * s;
                retVal.diag_[i] = diag_[i] * s;
                retVal.upper_[i] = upper_[i] * s;
            }

            return retVal;
        }

        public TripleBandLinearOp multR(Vector u)
        {
            var layout = mesher_.layout();
            var size = layout.size();
            Utils.QL_REQUIRE(u.size() == size, () => "inconsistent size of rhs");
            var retVal = new TripleBandLinearOp(direction_, mesher_);

            for (var i = 0; i < size; ++i)
            {
                var sm1 = i > 0 ? u[i - 1] : 1.0;
                var s0 = u[i];
                var sp1 = i < size - 1 ? u[i + 1] : 1.0;
                retVal.lower_[i] = lower_[i] * sm1;
                retVal.diag_[i] = diag_[i] * s0;
                retVal.upper_[i] = upper_[i] * sp1;
            }

            return retVal;
        }

        public Vector solve_splitting(Vector r, double a, double b = 1.0)
        {
            var layout = mesher_.layout();
            Utils.QL_REQUIRE(r.size() == layout.size(), () => "inconsistent size of rhs");

            for (var iter = layout.begin();
                 iter != layout.end();
                 ++iter)
            {
                var coordinates = iter.coordinates();
                Utils.QL_REQUIRE(coordinates[direction_] != 0
                                 || lower_[iter.index()] == 0, () => "removing non zero entry!");
                Utils.QL_REQUIRE(coordinates[direction_] != layout.dim()[direction_] - 1
                                 || upper_[iter.index()] == 0, () => "removing non zero entry!");
            }

            Vector retVal = new Vector(r.size()), tmp = new Vector(r.size());

            // Thomson algorithm to solve a tridiagonal system.
            // Example code taken from Tridiagonalopertor and
            // changed to fit for the triple band operator.
            var rim1 = reverseIndex_[0];
            var bet = 1.0 / (a * diag_[rim1] + b);
            Utils.QL_REQUIRE(bet != 0.0, () => "division by zero");
            retVal[reverseIndex_[0]] = r[rim1] * bet;

            for (var j = 1; j <= layout.size() - 1; j++)
            {
                var ri = reverseIndex_[j];
                tmp[j] = a * upper_[rim1] * bet;

                bet = b + a * (diag_[ri] - tmp[j] * lower_[ri]);
                Utils.QL_REQUIRE(bet != 0.0, () => "division by zero"); //QL_ENSURE
                bet = 1.0 / bet;

                retVal[ri] = (r[ri] - a * lower_[ri] * retVal[rim1]) * bet;
                rim1 = ri;
            }

            // cannot be j>=0 with Size j
            for (var j = layout.size() - 2; j > 0; --j)
            {
                retVal[reverseIndex_[j]] -= tmp[j + 1] * retVal[reverseIndex_[j + 1]];
            }

            retVal[reverseIndex_[0]] -= tmp[1] * retVal[reverseIndex_[1]];

            return retVal;
        }

        public void swap(TripleBandLinearOp m)
        {
            Utils.swap(ref mesher_, ref m.mesher_);
            Utils.swap(ref direction_, ref m.direction_);
            Utils.swap(ref i0_, ref m.i0_);
            Utils.swap(ref i2_, ref m.i2_);
            Utils.swap(ref reverseIndex_, ref m.reverseIndex_);
            Utils.swap(ref lower_, ref m.lower_);
            Utils.swap(ref diag_, ref m.diag_);
            Utils.swap(ref upper_, ref m.upper_);
        }

        public override SparseMatrix toMatrix()
        {
            var index = mesher_.layout();
            var n = index.size();

            var retVal = new SparseMatrix(n, n);
            for (var i = 0; i < n; ++i)
            {
                retVal[i, i0_[i]] += lower_[i];
                retVal[i, i] += diag_[i];
                retVal[i, i2_[i]] += upper_[i];
            }

            return retVal;
        }

        #region IOperator interface

        public override int size() => 0;

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
