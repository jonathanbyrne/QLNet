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

    //! Intermediate class for payoffs based on a fixed strike

    //! Plain-vanilla payoff

    //! %Payoff with strike expressed as percentage

    /*! Definitions of Binary path-independent payoffs used below,
        can be found in M. Rubinstein, E. Reiner:"Unscrambling The Binary Code", Risk, Vol.4 no.9,1991.
        (see: http://www.in-the-money.com/artandpap/Binary%20Options.doc)
    */
    //! Binary asset-or-nothing payoff

    //! Binary cash-or-nothing payoff

    //! Binary gap payoff
    /*! This payoff is equivalent to being a) long a PlainVanillaPayoff at
        the first strike (same Call/Put ExerciseType) and b) short a
        CashOrNothingPayoff at the first strike (same Call/Put ExerciseType) with
        cash payoff equal to the difference between the second and the first
        strike.
        \warning this payoff can be negative depending on the strikes
    */

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

    //! Binary supershare payoff
}
