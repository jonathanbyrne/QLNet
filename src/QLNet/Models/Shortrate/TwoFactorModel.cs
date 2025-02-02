﻿/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)

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
using JetBrains.Annotations;
using QLNet.Methods.lattices;

namespace QLNet.Models.Shortrate
{
    public abstract class TwoFactorModel : ShortRateModel
    {
        //! Class describing the dynamics of the two state variables
        /*! We assume here that the short-rate is a function of two state
            variables x and y.
        */

        public abstract class ShortRateDynamics
        {
            private readonly StochasticProcess1D xProcess_;
            private readonly StochasticProcess1D yProcess_;

            protected ShortRateDynamics(StochasticProcess1D xProcess,
                StochasticProcess1D yProcess,
                double correlation)
            {
                xProcess_ = xProcess;
                yProcess_ = yProcess;
                correlation_ = correlation;
            }

            public double correlation_ { get; set; }

            //! Joint process of the two variables
            public abstract StochasticProcess process();

            public abstract double shortRate(double t, double x, double y);

            //! Correlation \f$ \rho \f$ between the two brownian motions.
            public double correlation() => correlation_;

            //! Risk-neutral dynamics of the first state variable x
            public StochasticProcess1D xProcess() => xProcess_;

            //! Risk-neutral dynamics of the second state variable y
            public StochasticProcess1D yProcess() => yProcess_;
        }

        //! Recombining two-dimensional tree discretizing the state variable
        [PublicAPI]
        public class ShortRateTree : TreeLattice2D<ShortRateTree, TrinomialTree>, IGenericLattice
        {
            private ShortRateDynamics dynamics_;

            //! Plain tree build-up from short-rate dynamics
            public ShortRateTree(TrinomialTree tree1,
                TrinomialTree tree2,
                ShortRateDynamics dynamics)
                : base(tree1, tree2, dynamics.correlation())
            {
                dynamics_ = dynamics;
            }

            public double discount(int i, int index)
            {
                var modulo = tree1_.size(i);
                var index1 = index % modulo;
                var index2 = index / modulo;

                var x = tree1_.underlying(i, index1);
                var y = tree2_.underlying(i, index2);

                var r = dynamics_.shortRate(timeGrid()[i], x, y);
                return System.Math.Exp(-r * timeGrid().dt(i));
            }

            #region Interface

            public double underlying(int i, int index) => throw new NotImplementedException();

            #endregion

            protected override ShortRateTree impl() => this;
        }

        protected TwoFactorModel(int nArguments)
            : base(nArguments)
        {
        }

        public abstract ShortRateDynamics dynamics();

        public override Lattice tree(TimeGrid grid)
        {
            var dyn = dynamics();
            var tree1 = new TrinomialTree(dyn.xProcess(), grid);
            var tree2 = new TrinomialTree(dyn.yProcess(), grid);
            return new ShortRateTree(tree1, tree2, dyn);
        }
    }
}
