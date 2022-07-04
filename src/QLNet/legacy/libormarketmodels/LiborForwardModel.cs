/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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
using QLNet.Indexes;
using QLNet.Math;
using QLNet.Models;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet.legacy.libormarketmodels
{
    [JetBrains.Annotations.PublicAPI] public class LiborForwardModel : CalibratedModel, IAffineModel
    {
        List<double> f_;
        List<double> accrualPeriod_;

        LfmCovarianceProxy covarProxy_;
        LiborForwardModelProcess process_;
        SwaptionVolatilityMatrix swaptionVola;

        public LiborForwardModel(LiborForwardModelProcess process,
                                 LmVolatilityModel volaModel,
                                 LmCorrelationModel corrModel)
           : base(volaModel.parameters().Count + corrModel.parameters().Count)
        {

            f_ = new InitializedList<double>(process.size());
            accrualPeriod_ = new InitializedList<double>(process.size());
            covarProxy_ = new LfmCovarianceProxy(volaModel, corrModel);
            process_ = process;

            var k = volaModel.parameters().Count;
            for (var j = 0; j < k; j++)
                arguments_[j] = volaModel.parameters()[j];
            for (var j = 0; j < corrModel.parameters().Count; j++)
                arguments_[j + k] = corrModel.parameters()[j];

            for (var i = 0; i < process.size(); ++i)
            {
                accrualPeriod_[i] = process.accrualEndTimes()[i]
                                     - process.accrualStartTimes()[i];
                f_[i] = 1.0 / (1.0 + accrualPeriod_[i] * process_.initialValues()[i]);
            }
        }



        public override void setParams(Vector parameters)
        {
            base.setParams(parameters);

            var k = covarProxy_.volatilityModel().parameters().Count;

            covarProxy_.volatilityModel().setParams(new List<Parameter>(arguments_.GetRange(0, k)));
            covarProxy_.correlationModel().setParams(new List<Parameter>(arguments_.GetRange(k, arguments_.Count - k)));

            swaptionVola = null;
        }


        public double discountBondOption(Option.Type type,
                                         double strike, double maturity,
                                         double bondMaturity)
        {

            var accrualStartTimes
               = process_.accrualStartTimes();
            var accrualEndTimes
               = process_.accrualEndTimes();

            Utils.QL_REQUIRE(accrualStartTimes.First() <= maturity && accrualStartTimes.Last() >= maturity, () =>
                             "capet maturity does not fit to the process");

            var i = accrualStartTimes.BinarySearch(maturity);
            if (i < 0)
                // The lower_bound() algorithm finds the first position in a sequence that value can occupy
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the prev minor item is returned
                i = ~i + 1;

            // impose limits. we need the one before last at max or the first at min
            i = System.Math.Max(System.Math.Min(i, accrualStartTimes.Count - 1), 0);

            Utils.QL_REQUIRE(i < process_.size()
                             && System.Math.Abs(maturity - accrualStartTimes[i]) < 100 * Const.QL_EPSILON
                             && System.Math.Abs(bondMaturity - accrualEndTimes[i]) < 100 * Const.QL_EPSILON, () =>
                             "irregular fixings are not (yet) supported");

            var tenor = accrualEndTimes[i] - accrualStartTimes[i];
            var forward = process_.initialValues()[i];
            var capRate = (1.0 / strike - 1.0) / tenor;
            var var = covarProxy_.integratedCovariance(i, i, process_.fixingTimes()[i]);
            var dis = process_.index().forwardingTermStructure().link.discount(bondMaturity);

            var black = Utils.blackFormula(
                              type == QLNet.Option.Type.Put ? QLNet.Option.Type.Call : QLNet.Option.Type.Put,
                              capRate, forward, System.Math.Sqrt(var));

            var npv = dis * tenor * black;

            return npv / (1.0 + capRate * tenor);
        }

        public double discount(double t) => process_.index().forwardingTermStructure().link.discount(t);

        public double discountBond(double t, double maturity, Vector v) => discount(maturity);

        public Vector w_0(int alpha, int beta)
        {
            var omega = new Vector(beta + 1, 0.0);
            Utils.QL_REQUIRE(alpha < beta, () => "alpha needs to be smaller than beta");

            var s = 0.0;
            for (var k = alpha + 1; k <= beta; ++k)
            {
                var b = accrualPeriod_[k];
                for (var j = alpha + 1; j <= k; ++j)
                {
                    b *= f_[j];
                }
                s += b;
            }

            for (var i = alpha + 1; i <= beta; ++i)
            {
                var a = accrualPeriod_[i];
                for (var j = alpha + 1; j <= i; ++j)
                {
                    a *= f_[j];
                }
                omega[i] = a / s;
            }
            return omega;
        }

        public double S_0(int alpha, int beta)
        {
            var w = w_0(alpha, beta);
            var f = process_.initialValues();

            var fwdRate = 0.0;
            for (var i = alpha + 1; i <= beta; ++i)
            {
                fwdRate += w[i] * f[i];
            }
            return fwdRate;
        }


        // calculating swaption volatility matrix using
        // Rebonatos approx. formula. Be aware that this
        // matrix is valid only for regular fixings and
        // assumes that the fix and floating leg have the
        // same frequency
        public SwaptionVolatilityMatrix getSwaptionVolatilityMatrix()
        {
            if (swaptionVola != null)
            {
                return swaptionVola;
            }

            var index = process_.index();
            var today = process_.fixingDates()[0];

            var size = process_.size() / 2;
            var volatilities = new Matrix(size, size);

            List<Date> exercises = new InitializedList<Date>(size);
            for (var i = 0; i < size; ++i)
            {
                exercises[i] = process_.fixingDates()[i + 1];
            }

            List<Period> lengths = new InitializedList<Period>(size);
            for (var i = 0; i < size; ++i)
            {
                lengths[i] = (i + 1) * index.tenor();
            }

            var f = process_.initialValues();
            for (var k = 0; k < size; ++k)
            {
                var alpha = k;
                var t_alpha = process_.fixingTimes()[alpha + 1];

                var var = new Matrix(size, size);
                for (var i = alpha + 1; i <= k + size; ++i)
                {
                    for (var j = i; j <= k + size; ++j)
                    {
                        var[i - alpha - 1, j - alpha - 1] = var[j - alpha - 1, i - alpha - 1] =
                                                               covarProxy_.integratedCovariance(i, j, t_alpha, null);
                    }
                }

                for (var l = 1; l <= size; ++l)
                {
                    var beta = l + k;
                    var w = w_0(alpha, beta);

                    var sum = 0.0;
                    for (var i = alpha + 1; i <= beta; ++i)
                    {
                        for (var j = alpha + 1; j <= beta; ++j)
                        {
                            sum += w[i] * w[j] * f[i] * f[j] * var[i - alpha - 1, j - alpha - 1];
                        }
                    }
                    volatilities[k, l - 1] =
                       System.Math.Sqrt(sum / t_alpha) / S_0(alpha, beta);
                }
            }

            return swaptionVola = new SwaptionVolatilityMatrix(today, exercises, lengths,
                                                               volatilities, index.dayCounter());
        }
    }
}
