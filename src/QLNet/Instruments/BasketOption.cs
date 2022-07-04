/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
using System.Linq;

namespace QLNet.Instruments
{
    public abstract class BasketPayoff : Payoff
    {
        private Payoff basePayoff_;

        protected BasketPayoff(Payoff p)
        {
            basePayoff_ = p;
        }

        public override string name() => basePayoff_.name();

        public override string description() => basePayoff_.description();

        public override double value(double price) => basePayoff_.value(price);

        public virtual double value(Vector a) => basePayoff_.value(accumulate(a));

        public abstract double accumulate(Vector a);

        public Payoff basePayoff() => basePayoff_;
    }

    [JetBrains.Annotations.PublicAPI] public class MinBasketPayoff : BasketPayoff
    {
        public MinBasketPayoff(Payoff p) : base(p)
        { }

        public override double accumulate(Vector a) => a.Min();
    }

    [JetBrains.Annotations.PublicAPI] public class MaxBasketPayoff : BasketPayoff
    {
        public MaxBasketPayoff(Payoff p) : base(p)
        { }

        public override double accumulate(Vector a) => a.Max();
    }

    [JetBrains.Annotations.PublicAPI] public class AverageBasketPayoff : BasketPayoff
    {
        public AverageBasketPayoff(Payoff p, Vector a) : base(p)
        {
            weights_ = a;
        }

        public AverageBasketPayoff(Payoff p, int n) : base(p)
        {
            weights_ = new Vector(n, 1.0 / n);
        }

        public override double accumulate(Vector a)
        {
            var tally = weights_ * a;
            return tally;
        }

        private Vector weights_;
    }

    [JetBrains.Annotations.PublicAPI] public class SpreadBasketPayoff : BasketPayoff
    {
        public SpreadBasketPayoff(Payoff p)
           : base(p)
        { }

        public override double accumulate(Vector a)
        {
            Utils.QL_REQUIRE(a.size() == 2, () => "payoff is only defined for two underlyings");
            return a[0] - a[1];
        }
    }

    //! Basket option on a number of assets
    //! \ingroup instruments
    [JetBrains.Annotations.PublicAPI] public class BasketOption : MultiAssetOption
    {
        public new class Engine : GenericEngine<Arguments, Results>
        { }

        public BasketOption(BasketPayoff payoff, Exercise exercise) : base(payoff, exercise)
        { }
    }
}
