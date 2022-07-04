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

namespace QLNet.Methods.lattices
{
    // factory to create exact versions of trees

    // interface for all trees

    //! Binomial tree base class
    /*! \ingroup lattices */

    public abstract class BinomialTree<T> : Tree<T>, ITree
    {
        public enum Branches
        {
            branches = 2
        }

        protected double x0_, driftPerStep_;
        protected double dt_;

        // parameterless constructor is requried for generics
        protected BinomialTree()
        { }

        protected BinomialTree(StochasticProcess1D process, double end, int steps)
           : base(steps + 1)
        {
            x0_ = process.x0();
            dt_ = end / steps;
            driftPerStep_ = process.drift(0.0, x0_) * dt_;
        }

        public int size(int i) => i + 1;

        public int descendant(int x, int index, int branch) => index + branch;

        public abstract double underlying(int i, int index);
        public abstract double probability(int x, int y, int z);
    }

    //! Base class for equal probabilities binomial tree
    /*! \ingroup lattices */

    //! Base class for equal jumps binomial tree
    /*! \ingroup lattices */

    //! Jarrow-Rudd (multiplicative) equal probabilities binomial tree
    /*! \ingroup lattices */

    //! Cox-Ross-Rubinstein (multiplicative) equal jumps binomial tree
    /*! \ingroup lattices */

    //! Additive equal probabilities binomial tree
    /*! \ingroup lattices */

    //! %Trigeorgis (additive equal jumps) binomial tree
    /*! \ingroup lattices */

    //! %Tian tree: third moment matching, multiplicative approach
    /*! \ingroup lattices */

    //! Leisen & Reimer tree: multiplicative approach
    /*! \ingroup lattices */
}
