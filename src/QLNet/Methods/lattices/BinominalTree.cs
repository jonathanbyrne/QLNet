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

namespace QLNet.Methods.lattices
{
    // factory to create exact versions of trees
    [JetBrains.Annotations.PublicAPI] public interface ITreeFactory<T>
    {
        T factory(StochasticProcess1D process, double end, int steps, double strike);
    }

    // interface for all trees
    [JetBrains.Annotations.PublicAPI] public interface ITree
    {
        int size(int i);
        int descendant(int x, int index, int branch);
        double underlying(int i, int index);
        double probability(int x, int y, int z);
    }

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

    [JetBrains.Annotations.PublicAPI] public class EqualProbabilitiesBinomialTree<T> : BinomialTree<T>
    {
        protected double up_;

        // parameterless constructor is requried for generics
        public EqualProbabilitiesBinomialTree()
        { }

        public EqualProbabilitiesBinomialTree(StochasticProcess1D process, double end, int steps)
           : base(process, end, steps)
        { }

        public override double underlying(int i, int index)
        {
            long j = 2 * index - i;
            // exploiting the forward value tree centering
            return x0_ * System.Math.Exp(i * driftPerStep_ + j * up_);
        }

        public override double probability(int x, int y, int z) => 0.5;
    }

    //! Base class for equal jumps binomial tree
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class EqualJumpsBinomialTree<T> : BinomialTree<T>
    {
        protected double dx_, pu_, pd_;

        // parameterless constructor is requried for generics
        public EqualJumpsBinomialTree()
        { }

        public EqualJumpsBinomialTree(StochasticProcess1D process, double end, int steps)
           : base(process, end, steps)
        { }

        public override double underlying(int i, int index)
        {
            long j = 2 * index - i;
            // exploiting equal jump and the x0_ tree centering
            return x0_ * System.Math.Exp(j * dx_);
        }

        public override double probability(int x, int y, int branch) => branch == 1 ? pu_ : pd_;
    }

    //! Jarrow-Rudd (multiplicative) equal probabilities binomial tree
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class JarrowRudd : EqualProbabilitiesBinomialTree<JarrowRudd>, ITreeFactory<JarrowRudd>
    {
        // parameterless constructor is requried for generics
        public JarrowRudd()
        { }

        public JarrowRudd(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps)
        {
            // drift removed
            up_ = process.stdDeviation(0.0, x0_, dt_);
        }

        public JarrowRudd factory(StochasticProcess1D process, double end, int steps, double strike) => new JarrowRudd(process, end, steps, strike);
    }

    //! Cox-Ross-Rubinstein (multiplicative) equal jumps binomial tree
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class CoxRossRubinstein : EqualJumpsBinomialTree<CoxRossRubinstein>, ITreeFactory<CoxRossRubinstein>
    {
        // parameterless constructor is requried for generics
        public CoxRossRubinstein()
        { }

        public CoxRossRubinstein(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps)
        {
            dx_ = process.stdDeviation(0.0, x0_, dt_);
            pu_ = 0.5 + 0.5 * driftPerStep_ / dx_;
            pd_ = 1.0 - pu_;

            Utils.QL_REQUIRE(pu_ <= 1.0, () => "negative probability");
            Utils.QL_REQUIRE(pu_ >= 0.0, () => "negative probability");
        }

        public CoxRossRubinstein factory(StochasticProcess1D process, double end, int steps, double strike) => new CoxRossRubinstein(process, end, steps, strike);
    }

    //! Additive equal probabilities binomial tree
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class AdditiveEQPBinomialTree : EqualProbabilitiesBinomialTree<AdditiveEQPBinomialTree>,
       ITreeFactory<AdditiveEQPBinomialTree>
    {
        // parameterless constructor is requried for generics
        public AdditiveEQPBinomialTree()
        { }

        public AdditiveEQPBinomialTree(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps)
        {
            up_ = -0.5 * driftPerStep_ +
                  0.5 * System.Math.Sqrt(4.0 * process.variance(0.0, x0_, dt_) - 3.0 * driftPerStep_ * driftPerStep_);
        }

        public AdditiveEQPBinomialTree factory(StochasticProcess1D process, double end, int steps, double strike) => new AdditiveEQPBinomialTree(process, end, steps, strike);
    }

    //! %Trigeorgis (additive equal jumps) binomial tree
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class Trigeorgis : EqualJumpsBinomialTree<Trigeorgis>, ITreeFactory<Trigeorgis>
    {
        // parameterless constructor is requried for generics
        public Trigeorgis()
        { }

        public Trigeorgis(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps)
        {
            dx_ = System.Math.Sqrt(process.variance(0.0, x0_, dt_) + driftPerStep_ * driftPerStep_);
            pu_ = 0.5 + 0.5 * driftPerStep_ / dx_;
            pd_ = 1.0 - pu_;

            Utils.QL_REQUIRE(pu_ <= 1.0, () => "negative probability");
            Utils.QL_REQUIRE(pu_ >= 0.0, () => "negative probability");
        }

        public Trigeorgis factory(StochasticProcess1D process, double end, int steps, double strike) => new Trigeorgis(process, end, steps, strike);
    }

    //! %Tian tree: third moment matching, multiplicative approach
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class Tian : BinomialTree<Tian>, ITreeFactory<Tian>
    {
        protected double up_, down_, pu_, pd_;

        // parameterless constructor is requried for generics
        public Tian()
        { }

        public Tian(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps)
        {
            var q = System.Math.Exp(process.variance(0.0, x0_, dt_));
            var r = System.Math.Exp(driftPerStep_) * System.Math.Sqrt(q);

            up_ = 0.5 * r * q * (q + 1 + System.Math.Sqrt(q * q + 2 * q - 3));
            down_ = 0.5 * r * q * (q + 1 - System.Math.Sqrt(q * q + 2 * q - 3));

            pu_ = (r - down_) / (up_ - down_);
            pd_ = 1.0 - pu_;

            Utils.QL_REQUIRE(pu_ <= 1.0, () => "negative probability");
            Utils.QL_REQUIRE(pu_ >= 0.0, () => "negative probability");
        }

        public override double underlying(int i, int index) => x0_ * System.Math.Pow(down_, i - index) * System.Math.Pow(up_, index);

        public override double probability(int i, int j, int branch) => branch == 1 ? pu_ : pd_;

        public Tian factory(StochasticProcess1D process, double end, int steps, double strike) => new Tian(process, end, steps, strike);
    }

    //! Leisen & Reimer tree: multiplicative approach
    /*! \ingroup lattices */

    [JetBrains.Annotations.PublicAPI] public class LeisenReimer : BinomialTree<LeisenReimer>, ITreeFactory<LeisenReimer>
    {
        protected double up_, down_, pu_, pd_;

        // parameterless constructor is requried for generics
        public LeisenReimer()
        { }

        public LeisenReimer(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps % 2 != 0 ? steps : steps + 1)
        {
            Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive");
            var oddSteps = steps % 2 != 0 ? steps : steps + 1;
            var variance = process.variance(0.0, x0_, end);
            var ermqdt = System.Math.Exp(driftPerStep_ + 0.5 * variance / oddSteps);
            var d2 = (System.Math.Log(x0_ / strike) + driftPerStep_ * oddSteps) / System.Math.Sqrt(variance);
            pu_ = Utils.PeizerPrattMethod2Inversion(d2, oddSteps);
            pd_ = 1.0 - pu_;
            var pdash = Utils.PeizerPrattMethod2Inversion(d2 + System.Math.Sqrt(variance), oddSteps);
            up_ = ermqdt * pdash / pu_;
            down_ = (ermqdt - pu_ * up_) / (1.0 - pu_);
        }

        public override double underlying(int i, int index) => x0_ * System.Math.Pow(down_, i - index) * System.Math.Pow(up_, index);

        public override double probability(int i, int j, int branch) => branch == 1 ? pu_ : pd_;

        public LeisenReimer factory(StochasticProcess1D process, double end, int steps, double strike) => new LeisenReimer(process, end, steps, strike);
    }

    [JetBrains.Annotations.PublicAPI] public class Joshi4 : BinomialTree<Joshi4>, ITreeFactory<Joshi4>
    {
        protected double up_, down_, pu_, pd_;

        // parameterless constructor is requried for generics
        public Joshi4()
        { }

        public Joshi4(StochasticProcess1D process, double end, int steps, double strike)
           : base(process, end, steps % 2 != 0 ? steps : steps + 1)
        {
            Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive");

            var oddSteps = steps % 2 != 0 ? steps : steps + 1;
            var variance = process.variance(0.0, x0_, end);
            var ermqdt = System.Math.Exp(driftPerStep_ + 0.5 * variance / oddSteps);
            var d2 = (System.Math.Log(x0_ / strike) + driftPerStep_ * oddSteps) / System.Math.Sqrt(variance);
            pu_ = computeUpProb((oddSteps - 1.0) / 2.0, d2);
            pd_ = 1.0 - pu_;
            var pdash = computeUpProb((oddSteps - 1.0) / 2.0, d2 + System.Math.Sqrt(variance));
            up_ = ermqdt * pdash / pu_;
            down_ = (ermqdt - pu_ * up_) / (1.0 - pu_);
        }

        public override double underlying(int i, int index) => x0_ * System.Math.Pow(down_, i - index) * System.Math.Pow(up_, index);

        public override double probability(int x, int y, int branch) => branch == 1 ? pu_ : pd_;

        protected double computeUpProb(double k, double dj)
        {
            var alpha = dj / System.Math.Sqrt(8.0);
            var alpha2 = alpha * alpha;
            var alpha3 = alpha * alpha2;
            var alpha5 = alpha3 * alpha2;
            var alpha7 = alpha5 * alpha2;
            var beta = -0.375 * alpha - alpha3;
            var gamma = 5.0 / 6.0 * alpha5 + 13.0 / 12.0 * alpha3
                                           + 25.0 / 128.0 * alpha;
            var delta = -0.1025 * alpha - 0.9285 * alpha3
                                        - 1.43 * alpha5 - 0.5 * alpha7;
            var p = 0.5;
            var rootk = System.Math.Sqrt(k);
            p += alpha / rootk;
            p += beta / (k * rootk);
            p += gamma / (k * k * rootk);
            // delete next line to get results for j three tree
            p += delta / (k * k * k * rootk);
            return p;
        }

        public Joshi4 factory(StochasticProcess1D process, double end, int steps, double strike) => new Joshi4(process, end, steps, strike);
    }
}
