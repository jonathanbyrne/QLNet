﻿/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Math.integrals;
using QLNet.Math.RandomNumbers;

namespace QLNet.Methods.montecarlo
{
    public static class LsmBasisSystem
    {
        public enum PolynomType
        {
            Monomial,
            Laguerre,
            Hermite,
            Hyperbolic,
            Legendre,
            Chebyshev,
            Chebyshev2th
        }

        public static List<Func<Vector, double>> multiPathBasisSystem(int dim, int order, PolynomType polynomType)
        {
            var b = pathBasisSystem(order, polynomType);

            var ret = new List<Func<Vector, double>>();
            ret.Add(xx => 1.0);

            for (var i = 1; i <= order; ++i)
            {
                var a = w(dim, i, polynomType, b);

                foreach (var iter in a)
                {
                    ret.Add(iter);
                }
            }

            // remove-o-zap: now remove redundant functions.
            // usually we do have a lot of them due to the construction schema.
            // We use a more "hands on" method here.
            List<bool> rm = new InitializedList<bool>(ret.Count, true);

            Vector x = new Vector(dim), v = new Vector(ret.Count);
            var rng = new MersenneTwisterUniformRng(1234UL);

            for (var i = 0; i < 10; ++i)
            {
                int k;

                // calculate random x vector
                for (k = 0; k < dim; ++k)
                {
                    x[k] = rng.next().value;
                }

                // get return values for all basis functions
                for (k = 0; k < ret.Count; ++k)
                {
                    v[k] = ret[k](x);
                }

                // find duplicates
                for (k = 0; k < ret.Count; ++k)
                {
                    if (v.First(xx => System.Math.Abs(v[k] - xx) <= 10 * v[k] * Const.QL_EPSILON).IsEqual(v.First() + k))
                    {
                        // don't remove this item, it's unique!
                        rm[k] = false;
                    }
                }
            }

            var iter2 = 0;
            for (var i = 0; i < rm.Count; ++i)
            {
                if (rm[i])
                {
                    ret.RemoveAt(iter2);
                }
                else
                {
                    ++iter2;
                }
            }

            return ret;
        }

        public static List<Func<double, double>> pathBasisSystem(int order, PolynomType polynomType)
        {
            var ret = new List<Func<double, double>>();
            for (var i = 0; i <= order; ++i)
            {
                switch (polynomType)
                {
                    case PolynomType.Monomial:
                        ret.Add(new MonomialFct(i).value);
                        break;
                    case PolynomType.Laguerre:
                        ret.Add(x => new GaussLaguerrePolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Hermite:
                        ret.Add(x => new GaussHermitePolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Hyperbolic:
                        ret.Add(x => new GaussHyperbolicPolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Legendre:
                        ret.Add(x => new GaussLegendrePolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Chebyshev:
                        ret.Add(x => new GaussChebyshevPolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Chebyshev2th:
                        ret.Add(x => new GaussChebyshev2ndPolynomial().weightedValue(i, x));
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("unknown regression ExerciseType");
                        break;
                }
            }

            return ret;
        }

        private static List<Func<Vector, double>> w(int dim, int order, PolynomType polynomType, List<Func<double, double>> b)
        {
            var ret = new List<Func<Vector, double>>();

            for (var i = order; i >= 1; --i)
            {
                var left = w(dim, order - i, polynomType, b);

                for (var j = 0; j < dim; ++j)
                {
                    Func<Vector, double> a = xx => b[i](xx[j]);

                    if (i == order)
                    {
                        ret.Add(a);
                    }
                    else // add linear combinations
                    {
                        for (j = 0; j < left.Count; ++j)
                        {
                            ret.Add(xx => a(xx * left[j](xx)));
                        }
                    }
                }
            }

            return ret;
        }
    }
}
