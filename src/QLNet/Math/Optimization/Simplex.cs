/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Extensions;

namespace QLNet.Math.Optimization
{
    //! Multi-dimensional simplex class
    [PublicAPI]
    public class Simplex : OptimizationMethod
    {
        private double lambda_;
        private Vector sum_;
        private Vector values_;
        private List<Vector> vertices_;

        //! Constructor taking as input the characteristic length
        public Simplex(double lambda)
        {
            lambda_ = lambda;
        }

        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
        {
            // set up of the problem
            //double ftol = endCriteria.functionEpsilon();    // end criteria on f(x) (see Numerical Recipes in C++, p.410)
            var xtol = endCriteria.rootEpsilon(); // end criteria on x (see GSL v. 1.9, http://www.gnu.org/software/gsl/)
            var maxStationaryStateIterations_ = endCriteria.maxStationaryStateIterations();
            var ecType = EndCriteria.Type.None;
            P.reset();
            var x_ = P.currentValue();
            var iterationNumber_ = 0;

            // Initialize vertices of the simplex
            var end = false;
            var n = x_.Count;
            vertices_ = new InitializedList<Vector>(n + 1, x_);
            for (var i = 0; i < n; i++)
            {
                var direction = new Vector(n, 0.0);
                var vertice = vertices_[i + 1];
                direction[i] = 1.0;
                P.constraint().update(ref vertice, direction, lambda_);
                vertices_[i + 1] = vertice;
            }

            // Initialize function values at the vertices of the simplex
            values_ = new Vector(n + 1, 0.0);
            for (var i = 0; i <= n; i++)
            {
                values_[i] = P.value(vertices_[i]);
            }

            // Loop looking for minimum
            do
            {
                sum_ = new Vector(n, 0.0);
                for (var i = 0; i <= n; i++)
                {
                    sum_ += vertices_[i];
                }

                // Determine the best (iLowest), worst (iHighest)
                // and 2nd worst (iNextHighest) vertices
                var iLowest = 0;
                int iHighest;
                int iNextHighest;
                if (values_[0] < values_[1])
                {
                    iHighest = 1;
                    iNextHighest = 0;
                }
                else
                {
                    iHighest = 0;
                    iNextHighest = 1;
                }

                for (var i = 1; i <= n; i++)
                {
                    if (values_[i] > values_[iHighest])
                    {
                        iNextHighest = iHighest;
                        iHighest = i;
                    }
                    else
                    {
                        if ((values_[i] > values_[iNextHighest]) && i != iHighest)
                        {
                            iNextHighest = i;
                        }
                    }

                    if (values_[i] < values_[iLowest])
                    {
                        iLowest = i;
                    }
                }

                // Now compute accuracy, update iteration number and check end criteria
                // GSL exit strategy on x (see GSL v. 1.9, http://www.gnu.org/software/gsl
                var simplexSize = Utils.computeSimplexSize(vertices_);
                ++iterationNumber_;
                if (simplexSize < xtol || endCriteria.checkMaxIterations(iterationNumber_, ref ecType))
                {
                    endCriteria.checkStationaryPoint(0.0, 0.0, ref maxStationaryStateIterations_, ref ecType);
                    endCriteria.checkMaxIterations(iterationNumber_, ref ecType);
                    x_ = vertices_[iLowest];
                    var low = values_[iLowest];
                    P.setFunctionValue(low);
                    P.setCurrentValue(x_);
                    return ecType;
                }

                // If end criteria is not met, continue
                var factor = -1.0;
                var vTry = extrapolate(ref P, iHighest, ref factor);
                if ((vTry <= values_[iLowest]) && (factor.IsEqual(-1.0)))
                {
                    factor = 2.0;
                    extrapolate(ref P, iHighest, ref factor);
                }
                else if (System.Math.Abs(factor) > Const.QL_EPSILON)
                {
                    if (vTry >= values_[iNextHighest])
                    {
                        var vSave = values_[iHighest];
                        factor = 0.5;
                        vTry = extrapolate(ref P, iHighest, ref factor);
                        if (vTry >= vSave && System.Math.Abs(factor) > Const.QL_EPSILON)
                        {
                            for (var i = 0; i <= n; i++)
                            {
                                if (i != iLowest)
                                {
#if QL_ARRAY_EXPRESSIONS
                           vertices_[i] = 0.5 * (vertices_[i] + vertices_[iLowest]);
#else
                                    vertices_[i] += vertices_[iLowest];
                                    vertices_[i] *= 0.5;
#endif
                                    values_[i] = P.value(vertices_[i]);
                                }
                            }
                        }
                    }
                }

                // If can't extrapolate given the constraints, exit
                if (System.Math.Abs(factor) <= Const.QL_EPSILON)
                {
                    x_ = vertices_[iLowest];
                    var low = values_[iLowest];
                    P.setFunctionValue(low);
                    P.setCurrentValue(x_);
                    return EndCriteria.Type.StationaryFunctionValue;
                }
            } while (end == false);

            QLNet.Utils.QL_FAIL("optimization failed: unexpected behaviour");
            return 0;
        }

        private double extrapolate(ref Problem P, int iHighest, ref double factor)
        {
            Vector pTry;
            do
            {
                var dimensions = values_.Count - 1;
                var factor1 = (1.0 - factor) / dimensions;
                var factor2 = factor1 - factor;
                pTry = sum_ * factor1 - vertices_[iHighest] * factor2;
                factor *= 0.5;
            } while (!P.constraint().test(pTry) && System.Math.Abs(factor) > Const.QL_EPSILON);

            if (System.Math.Abs(factor) <= Const.QL_EPSILON)
            {
                return values_[iHighest];
            }

            factor *= 2.0;
            var vTry = P.value(pTry);
            if (vTry < values_[iHighest])
            {
                values_[iHighest] = vTry;
                sum_ += pTry - vertices_[iHighest];
                vertices_[iHighest] = pTry;
            }

            return vTry;
        }
    }
}
