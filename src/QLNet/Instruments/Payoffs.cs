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

namespace QLNet.Instruments
{
    //! Intermediate class for put/call payoffs
    [JetBrains.Annotations.PublicAPI] public class TypePayoff : Payoff
    {
        protected QLNet.Option.Type type_;
        public QLNet.Option.Type optionType() => type_;

        public TypePayoff(Option.Type type)
        {
            type_ = type;
        }

        // Payoff interface
        public override string description() => name() + " " + optionType();
    }

    //! %Payoff based on a floating strike
    [JetBrains.Annotations.PublicAPI] public class FloatingTypePayoff : TypePayoff
    {
        public FloatingTypePayoff(Option.Type type) : base(type) { }

        // Payoff interface
        public override string name() => "FloatingType";

        public override double value(double k) => throw new NotSupportedException("floating payoff not handled");
    }

    //! Intermediate class for payoffs based on a fixed strike
    [JetBrains.Annotations.PublicAPI] public class StrikedTypePayoff : TypePayoff
    {
        protected double strike_;

        public StrikedTypePayoff(Option.Type type, double strike) : base(type)
        {
            strike_ = strike;
        }

        public StrikedTypePayoff(Payoff p)
           : base((p as StrikedTypePayoff).type_)
        {
            strike_ = (p as StrikedTypePayoff).strike_;
        }

        // Payoff interface
        public override string description() => base.description() + ", " + strike() + " strike";

        public double strike() => strike_;
    }

    //! Plain-vanilla payoff
    [JetBrains.Annotations.PublicAPI] public class PlainVanillaPayoff : StrikedTypePayoff
    {
        public PlainVanillaPayoff(Option.Type type, double strike) : base(type, strike) { }

        // Payoff interface
        public override string name() => "Vanilla";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return System.Math.Max(price - strike_, 0.0);
                case QLNet.Option.Type.Put:
                    return System.Math.Max(strike_ - price, 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }

    //! %Payoff with strike expressed as percentage
    [JetBrains.Annotations.PublicAPI] public class PercentageStrikePayoff : StrikedTypePayoff
    {
        public PercentageStrikePayoff(Option.Type type, double moneyness) : base(type, moneyness) { }

        // Payoff interface
        public override string name() => "PercentageStrike";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return price * System.Math.Max(1.0 - strike_, 0.0);
                case QLNet.Option.Type.Put:
                    return price * System.Math.Max(strike_ - 1.0, 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }

    /*! Definitions of Binary path-independent payoffs used below,
        can be found in M. Rubinstein, E. Reiner:"Unscrambling The Binary Code", Risk, Vol.4 no.9,1991.
        (see: http://www.in-the-money.com/artandpap/Binary%20Options.doc)
    */
    //! Binary asset-or-nothing payoff
    [JetBrains.Annotations.PublicAPI] public class AssetOrNothingPayoff : StrikedTypePayoff
    {
        public AssetOrNothingPayoff(Option.Type type, double strike) : base(type, strike) { }

        // Payoff interface
        public override string name() => "AssetOrNothing";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return price - strike_ > 0.0 ? price : 0.0;
                case QLNet.Option.Type.Put:
                    return strike_ - price > 0.0 ? price : 0.0;
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }

    //! Binary cash-or-nothing payoff
    [JetBrains.Annotations.PublicAPI] public class CashOrNothingPayoff : StrikedTypePayoff
    {
        protected double cashPayoff_;
        public double cashPayoff() => cashPayoff_;

        public CashOrNothingPayoff(Option.Type type, double strike, double cashPayoff) : base(type, strike)
        {
            cashPayoff_ = cashPayoff;
        }
        // Payoff interface
        public override string name() => "CashOrNothing";

        public override string description() => base.description() + ", " + cashPayoff() + " cash payoff";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return price - strike_ > 0.0 ? cashPayoff_ : 0.0;
                case QLNet.Option.Type.Put:
                    return strike_ - price > 0.0 ? cashPayoff_ : 0.0;
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }

    //! Binary gap payoff
    /*! This payoff is equivalent to being a) long a PlainVanillaPayoff at
        the first strike (same Call/Put ExerciseType) and b) short a
        CashOrNothingPayoff at the first strike (same Call/Put ExerciseType) with
        cash payoff equal to the difference between the second and the first
        strike.
        \warning this payoff can be negative depending on the strikes
    */
    [JetBrains.Annotations.PublicAPI] public class GapPayoff : StrikedTypePayoff
    {
        protected double secondStrike_;
        public double secondStrike() => secondStrike_;

        public GapPayoff(Option.Type type, double strike, double secondStrike) // a.k.a. payoff strike
           : base(type, strike)
        {
            secondStrike_ = secondStrike;
        }

        // Payoff interface
        public override string name() => "Gap";

        public override string description() => base.description() + ", " + secondStrike() + " strike payoff";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return price - strike_ >= 0.0 ? price - secondStrike_ : 0.0;
                case QLNet.Option.Type.Put:
                    return strike_ - price >= 0.0 ? secondStrike_ - price : 0.0;
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }

    //! Binary supershare and superfund payoffs

    //! Binary superfund payoff
    /*! Superfund sometimes also called "supershare", which can lead to ambiguity; within QuantLib
        the terms supershare and superfund are used consistently according to the definitions in
        Bloomberg OVX function's help pages.
    */
    /*! This payoff is equivalent to being (1/lowerstrike) a) long (short) an AssetOrNothing
        Call (Put) at the lower strike and b) short (long) an AssetOrNothing
        Call (Put) at the higher strike
    */
    [JetBrains.Annotations.PublicAPI] public class SuperFundPayoff : StrikedTypePayoff
    {
        protected double secondStrike_;
        public double secondStrike() => secondStrike_;

        public SuperFundPayoff(double strike, double secondStrike) : base(QLNet.Option.Type.Call, strike)
        {
            secondStrike_ = secondStrike;

            Utils.QL_REQUIRE(strike > 0.0, () => "strike (" + strike + ") must be positive");
            Utils.QL_REQUIRE(secondStrike > strike, () => "second strike (" + secondStrike +
                             ") must be higher than first strike (" + strike + ")");
        }

        // Payoff interface
        public override string name() => "SuperFund";

        public override double value(double price) => price >= strike_ && price < secondStrike_ ? price / strike_ : 0.0;
    }

    //! Binary supershare payoff
    [JetBrains.Annotations.PublicAPI] public class SuperSharePayoff : StrikedTypePayoff
    {
        protected double secondStrike_;
        public double secondStrike() => secondStrike_;

        protected double cashPayoff_;
        public double cashPayoff() => cashPayoff_;

        public SuperSharePayoff(double strike, double secondStrike, double cashPayoff)
           : base(QLNet.Option.Type.Call, strike)
        {
            secondStrike_ = secondStrike;
            cashPayoff_ = cashPayoff;

            Utils.QL_REQUIRE(secondStrike > strike, () => "second strike (" + secondStrike +
                             ") must be higher than first strike (" + strike + ")");
        }

        // Payoff interface
        public override string name() => "SuperShare";

        public override string description() => base.description() + ", " + secondStrike() + " second strike" + ", " + cashPayoff() + " amount";

        public override double value(double price) => price >= strike_ && price < secondStrike_ ? cashPayoff_ : 0.0;
    }
}
