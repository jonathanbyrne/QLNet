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
using QLNet.Math;
using QLNet.Math.Solvers1d;
using QLNet.Methods.lattices;
using QLNet.Termstructures;
using System;
using QLNet.Math.Optimization;
using QLNet.processes;

namespace QLNet.Models.Shortrate.Onefactormodels
{

    [JetBrains.Annotations.PublicAPI] public class BlackKarasinski : OneFactorModel,
       ITermStructureConsistentModel
    {
        private double a() => a_.value(0.0);

        private double sigma() => sigma_.value(0.0);

        Parameter a_;
        Parameter sigma_;

        public BlackKarasinski(Handle<YieldTermStructure> termStructure,
                               double a, double sigma)
           : base(2)
        {
            a_ = arguments_[0];
            sigma_ = arguments_[1];
            a_ = arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
            sigma_ = arguments_[1] = new ConstantParameter(sigma, new PositiveConstraint());
            termStructure_ = new Handle<YieldTermStructure>();
            termStructure_ = termStructure;
            termStructure.registerWith(update);
        }

        public BlackKarasinski(Handle<YieldTermStructure> termStructure)
           : this(termStructure, 0.1, 0.1)
        { }

        public override Lattice tree(TimeGrid grid)
        {
            var phi = new TermStructureFittingParameter(termStructure());

            ShortRateDynamics numericDynamics =
               new Dynamics(phi, a(), sigma());

            var trinomial =
               new TrinomialTree(numericDynamics.process(), grid);
            var numericTree =
               new ShortRateTree(trinomial, numericDynamics, grid);

            var impl =
               (TermStructureFittingParameter.NumericalImpl)phi.implementation();
            impl.reset();
            var value = 1.0;
            var vMin = -50.0;
            var vMax = 50.0;
            for (var i = 0; i < grid.size() - 1; i++)
            {
                var discountBond = termStructure().link.discount(grid[i + 1]);
                var xMin = trinomial.underlying(i, 0);
                var dx = trinomial.dx(i);
                var finder = new Helper(i, xMin, dx, discountBond, numericTree);
                var s1d = new Brent();
                s1d.setMaxEvaluations(1000);
                value = s1d.solve(finder, 1e-7, value, vMin, vMax);
                impl.setvalue(grid[i], value);
            }
            return numericTree;
        }

        public override ShortRateDynamics dynamics() => throw new NotImplementedException("no defined process for Black-Karasinski");

        #region ITermStructureConsistentModel
        public Handle<YieldTermStructure> termStructure() => termStructure_;

        public Handle<YieldTermStructure> termStructure_ { get; set; }

        #endregion
    }

    //! Short-rate dynamics in the Black-Karasinski model
    [JetBrains.Annotations.PublicAPI] public class Dynamics : OneFactorModel.ShortRateDynamics
    {

        public Dynamics(Parameter fitting, double alpha, double sigma)
           : base(new OrnsteinUhlenbeckProcess(alpha, sigma))
        {
            fitting_ = fitting;
        }

        public override double variable(double t, double r) => System.Math.Log(r) - fitting_.value(t);

        public override double shortRate(double t, double x) => System.Math.Exp(x + fitting_.value(t));

        private Parameter fitting_;
    }

    // Private function used by solver to determine time-dependent parameter
    [JetBrains.Annotations.PublicAPI] public class Helper : ISolver1d
    {
        private int size_;
        private double dt_;
        private double xMin_, dx_;
        private Vector statePrices_;
        private double discountBondPrice_;

        public Helper(int i, double xMin, double dx,
                      double discountBondPrice,
                      OneFactorModel.ShortRateTree tree)
        {
            size_ = tree.size(i);
            dt_ = tree.timeGrid().dt(i);
            xMin_ = xMin;
            dx_ = dx;
            statePrices_ = tree.statePrices(i);
            discountBondPrice_ = discountBondPrice;
        }

        public override double value(double theta)
        {
            var value = discountBondPrice_;
            var x = xMin_;
            for (var j = 0; j < size_; j++)
            {
                var discount = System.Math.Exp(-System.Math.Exp(theta + x) * dt_);
                value -= statePrices_[j] * discount;
                x += dx_;
            }
            return value;
        }
    }
}
