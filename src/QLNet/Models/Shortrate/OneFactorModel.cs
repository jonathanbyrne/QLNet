﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Methods.lattices;
using System;

namespace QLNet.Models.Shortrate
{
    //! Single-factor short-rate model abstract class
    /*! \ingroup shortrate */

    public abstract class OneFactorModel : ShortRateModel
    {
        protected OneFactorModel(int nArguments) : base(nArguments)
        { }

        //! Base class describing the short-rate dynamics
        public abstract class ShortRateDynamics
        {
            private StochasticProcess1D process_;
            //! Returns the risk-neutral dynamics of the state variable
            public StochasticProcess1D process() => process_;

            protected ShortRateDynamics(StochasticProcess1D process)
            {
                process_ = process;
            }

            //! Compute state variable from short rate
            public abstract double variable(double t, double r);

            //! Compute short rate from state variable
            public abstract double shortRate(double t, double variable);
        }

        //! returns the short-rate dynamics
        public abstract ShortRateDynamics dynamics();

        //! Return by default a trinomial recombining tree
        public override Lattice tree(TimeGrid grid)
        {
            var trinomial = new TrinomialTree(dynamics().process(), grid);
            return new ShortRateTree(trinomial, dynamics(), grid);
        }

        //! Recombining trinomial tree discretizing the state variable
        [JetBrains.Annotations.PublicAPI] public class ShortRateTree : TreeLattice1D<ShortRateTree>, IGenericLattice
        {
            protected override ShortRateTree impl() => this;

            //! Plain tree build-up from short-rate dynamics
            public ShortRateTree(TrinomialTree tree,
                                 ShortRateDynamics dynamics,
                                 TimeGrid timeGrid)
               : base(timeGrid, tree.size(1))
            {
                tree_ = tree;
                dynamics_ = dynamics;
            }

            //! Tree build-up + numerical fitting to term-structure
            public ShortRateTree(TrinomialTree tree, ShortRateDynamics dynamics, TermStructureFittingParameter.NumericalImpl theta,
                                 TimeGrid timeGrid)
               : base(timeGrid, tree.size(1))
            {
                tree_ = tree;
                dynamics_ = dynamics;
                theta.reset();
                var value = 1.0;
                var vMin = -100.0;
                var vMax = 100.0;
                for (var i = 0; i < timeGrid.size() - 1; i++)
                {
                    var discountBond = theta.termStructure().link.discount(t_[i + 1]);
                    var finder = new Helper(i, discountBond, theta, this);
                    var s1d = new Brent();
                    s1d.setMaxEvaluations(1000);
                    value = s1d.solve(finder, 1e-7, value, vMin, vMax);
                    theta.change(value);
                }
            }

            public int size(int i) => tree_.size(i);

            public double discount(int i, int index)
            {
                var x = tree_.underlying(i, index);
                var r = dynamics_.shortRate(timeGrid()[i], x);
                return System.Math.Exp(-r * timeGrid().dt(i));
            }

            public override double underlying(int i, int index) => tree_.underlying(i, index);

            public int descendant(int i, int index, int branch) => tree_.descendant(i, index, branch);

            public double probability(int i, int index, int branch) => tree_.probability(i, index, branch);

            private TrinomialTree tree_;
            private ShortRateDynamics dynamics_;

            [JetBrains.Annotations.PublicAPI] public class Helper : ISolver1d
            {
                private int size_;
                private int i_;
                private Vector statePrices_;
                private double discountBondPrice_;
                private TermStructureFittingParameter.NumericalImpl theta_;
                private ShortRateTree tree_;

                public Helper(int i,
                              double discountBondPrice,
                              TermStructureFittingParameter.NumericalImpl theta,
                              ShortRateTree tree)
                {
                    size_ = tree.size(i);
                    i_ = i;
                    statePrices_ = tree.statePrices(i);
                    discountBondPrice_ = discountBondPrice;
                    theta_ = theta;
                    tree_ = tree;
                    theta_.setvalue(tree.timeGrid()[i], 0.0);
                }

                public override double value(double theta)
                {
                    var value = discountBondPrice_;
                    theta_.change(theta);
                    for (var j = 0; j < size_; j++)
                        value -= statePrices_[j] * tree_.discount(i_, j);
                    return value;
                }
            }
        }
    }

    public abstract class OneFactorAffineModel : OneFactorModel,
       IAffineModel
    {
        protected OneFactorAffineModel(int nArguments)
           : base(nArguments)
        { }

        public virtual double discountBond(double now,
                                           double maturity,
                                           Vector factors) =>
            discountBond(now, maturity, factors[0]);

        public double discountBond(double now, double maturity, double rate) => A(now, maturity) * System.Math.Exp(-B(now, maturity) * rate);

        public double discount(double t)
        {
            var x0 = dynamics().process().x0();
            var r0 = dynamics().shortRate(0.0, x0);
            return discountBond(0.0, t, r0);
        }

        public virtual double discountBondOption(Option.Type type,
                                                 double strike,
                                                 double maturity,
                                                 double bondMaturity) =>
            throw new NotImplementedException();

        protected abstract double A(double t, double T);
        protected abstract double B(double t, double T);
    }
}
