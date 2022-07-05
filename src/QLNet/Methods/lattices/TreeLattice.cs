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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.lattices
{
    //! Tree-based lattice-method base class
    /*! This class defines a lattice method that is able to rollback
        (with discount) a discretized asset object. It will be based
        on one or more trees.
    */

    [PublicAPI]
    public class TreeLattice<T> : Lattice where T : IGenericLattice
    {
        // Arrow-Debrew state prices
        protected List<Vector> statePrices_;
        private int n_;
        private int statePricesLimit_;

        public TreeLattice(TimeGrid timeGrid, int n) : base(timeGrid)
        {
            n_ = n;
            QLNet.Utils.QL_REQUIRE(n > 0, () => "there is no zeronomial lattice!");
            statePrices_ = new InitializedList<Vector>(1, new Vector(1, 1.0));
            statePricesLimit_ = 0;
        }

        // Lattice interface
        public override void initialize(DiscretizedAsset asset, double t)
        {
            var i = t_.index(t);
            asset.setTime(t);
            asset.reset(impl().size(i));
        }

        public override void partialRollback(DiscretizedAsset asset, double to)
        {
            var from = asset.time();

            if (Math.Utils.close(from, to))
            {
                return;
            }

            QLNet.Utils.QL_REQUIRE(from > to, () => "cannot roll the asset back to" + to + " (it is already at t = " + from + ")");

            var iFrom = t_.index(from);
            var iTo = t_.index(to);

            for (var i = iFrom - 1; i >= iTo; --i)
            {
                var newValues = new Vector(impl().size(i));
                impl().stepback(i, asset.values(), newValues);
                asset.setTime(t_[i]);
                asset.setValues(newValues);
                // skip the very last adjustment
                if (i != iTo)
                {
                    asset.adjustValues();
                }
            }
        }

        //! Computes the present value of an asset using Arrow-Debrew prices
        public override double presentValue(DiscretizedAsset asset)
        {
            var i = t_.index(asset.time());
            return Vector.DotProduct(asset.values(), statePrices(i));
        }

        public override void rollback(DiscretizedAsset asset, double to)
        {
            partialRollback(asset, to);
            asset.adjustValues();
        }

        public Vector statePrices(int i)
        {
            if (i > statePricesLimit_)
            {
                computeStatePrices(i);
            }

            return statePrices_[i];
        }

        public virtual void stepback(int i, Vector values, Vector newValues)
        {
            for (var j = 0; j < impl().size(i); j++)
            {
                var value = 0.0;
                for (var l = 0; l < n_; l++)
                {
                    double d1, d2;
                    d1 = impl().probability(i, j, l);
                    d2 = values[impl().descendant(i, j, l)];
                    value += impl().probability(i, j, l) * values[impl().descendant(i, j, l)];
                }

                value *= impl().discount(i, j);
                newValues[j] = value;
            }
        }

        protected void computeStatePrices(int until)
        {
            for (var i = statePricesLimit_; i < until; i++)
            {
                statePrices_.Add(new Vector(impl().size(i + 1), 0.0));
                for (var j = 0; j < impl().size(i); j++)
                {
                    var disc = impl().discount(i, j);
                    var statePrice = statePrices_[i][j];
                    for (var l = 0; l < n_; l++)
                    {
                        statePrices_[i + 1][impl().descendant(i, j, l)] += statePrice * disc * impl().probability(i, j, l);
                    }
                }
            }

            statePricesLimit_ = until;
        }

        // this should be overriden in the dering class
        protected virtual T impl() => throw new NotSupportedException();
    }
}
