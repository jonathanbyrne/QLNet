﻿//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using JetBrains.Annotations;
using QLNet.Instruments.Bonds;
using QLNet.Math;

namespace QLNet.Methods.lattices
{
    /// <summary>
    ///     Binomial lattice approximating the Tsiveriotis-Fernandes model
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [PublicAPI]
    public class TsiveriotisFernandesLattice<T> : BlackScholesLattice<T> where T : ITree
    {
        private double creditSpread_;

        public TsiveriotisFernandesLattice(T tree, double riskFreeRate,
            double end, int steps, double creditSpread, double sigma, double divYield)
            : base(tree, riskFreeRate, end, steps)
        {
            creditSpread_ = creditSpread;
            QLNet.Utils.QL_REQUIRE(pu_ <= 1.0, () => "probability pu higher than one ");
            QLNet.Utils.QL_REQUIRE(pu_ >= 0.0, () => " negative pu probability ");
        }

        public override void partialRollback(DiscretizedAsset asset, double to)
        {
            var from = asset.time();

            if (Math.Utils.close(from, to))
            {
                return;
            }

            QLNet.Utils.QL_REQUIRE(from > to, () => " cannot roll the asset back to tile to it is already at time from ");

            var convertible = asset as DiscretizedConvertible;

            var iFrom = t_.index(from);
            var iTo = t_.index(to);

            for (var i = iFrom - 1; i >= iTo; --i)
            {
                var newValues = new Vector(size(i));
                var newSpreadAdjustedRate = new Vector(size(i));
                var newConversionProbability = new Vector(size(i));

                stepback(i, convertible.values(), convertible.conversionProbability(), convertible.spreadAdjustedRate(),
                    newValues, newConversionProbability, newSpreadAdjustedRate);
                convertible.setTime(t_[i]);
                convertible.setValues(newValues);
                convertible.spreadAdjustedRate_ = newSpreadAdjustedRate;
                convertible.conversionProbability_ = newConversionProbability;

                // skip the very last adjustement
                if (i != iTo)
                {
                    convertible.adjustValues();
                }
            }
        }

        public override void rollback(DiscretizedAsset asset, double to)
        {
            partialRollback(asset, to);
            asset.adjustValues();
        }

        public void stepback(int i, Vector values, Vector conversionProbability, Vector spreadAdjustedRate,
            Vector newValues, Vector newConversionProbability, Vector newSpreadAdjustedRate)
        {
            for (var j = 0; j < size(i); j++)
            {
                // new conversion probability is calculated via backward
                // induction using up and down probabilities on tree on
                // previous conversion probabilities, ie weighted average
                // of previous probabilities.
                newConversionProbability[j] =
                    pd_ * conversionProbability[j] + pu_ * conversionProbability[j + 1];

                // Use blended discounting rate
                newSpreadAdjustedRate[j] = newConversionProbability[j] * riskFreeRate_ +
                                           (1 - newConversionProbability[j]) * (riskFreeRate_ + creditSpread_);
                newValues[j] = pd_ * values[j] / (1 + spreadAdjustedRate[j] * dt_) +
                               pu_ * values[j + 1] / (1 + spreadAdjustedRate[j + 1] * dt_);
            }
        }
    }
}
