﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Methods.lattices
{
    // this is just a wrapper for QL compatibility

    //! Simple binomial lattice approximating the Black-Scholes model
    /*! \ingroup lattices */

    [PublicAPI]
    public class BlackScholesLattice : TreeLattice1D<BlackScholesLattice>, IGenericLattice
    {
        protected double discount_;
        protected double dt_;
        protected double pd_, pu_;
        protected double riskFreeRate_;
        protected ITree tree_;

        public BlackScholesLattice(ITree tree, double riskFreeRate, double end, int steps)
            : base(new TimeGrid(end, steps), 2)
        {
            tree_ = tree;
            riskFreeRate_ = riskFreeRate;
            dt_ = end / steps;
            discount_ = System.Math.Exp(-riskFreeRate * (end / steps));
            pd_ = tree.probability(0, 0, 0);
            pu_ = tree.probability(0, 0, 1);
        }

        public int descendant(int i, int index, int branch) => tree_.descendant(i, index, branch);

        public double discount(int i, int j) => discount_;

        public double dt() => dt_;

        public double probability(int i, int index, int branch) => tree_.probability(i, index, branch);

        public double riskFreeRate() => riskFreeRate_;

        public int size(int i) => tree_.size(i);

        public override void stepback(int i, Vector values, Vector newValues)
        {
            for (var j = 0; j < size(i); j++)
            {
                newValues[j] = (pd_ * values[j] + pu_ * values[j + 1]) * discount_;
            }
        }

        public override double underlying(int i, int index) => tree_.underlying(i, index);

        // this is a workaround for CuriouslyRecurringTemplate of TreeLattice
        // recheck it
        protected override BlackScholesLattice impl() => this;
    }
}
