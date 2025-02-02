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
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Methods.montecarlo;

namespace QLNet.Math.RandomNumbers
{
    //! Inverse cumulative random sequence generator
    /*! It uses a sequence of uniform deviate in (0, 1) as the
        source of cumulative distribution values.
        Then an inverse cumulative distribution is used to calculate
        the distribution deviate.

        The uniform deviate sequence is supplied by USG.
        The inverse cumulative distribution is supplied by IC.
    */

    [PublicAPI]
    public class InverseCumulativeRsg<USG, IC> : IRNG where USG : IRNG where IC : IValue
    {
        private int dimension_;
        private IC ICD_;
        private USG uniformSequenceGenerator_;
        private Sample<List<double>> x_;

        public InverseCumulativeRsg(USG uniformSequenceGenerator)
        {
            uniformSequenceGenerator_ = uniformSequenceGenerator;
            dimension_ = uniformSequenceGenerator_.dimension();
            x_ = new Sample<List<double>>(new InitializedList<double>(dimension_), 1.0);
        }

        public InverseCumulativeRsg(USG uniformSequenceGenerator, IC inverseCumulative) : this(uniformSequenceGenerator)
        {
            ICD_ = inverseCumulative;
        }

        #region IRNG interface

        //! returns next sample from the Gaussian distribution
        public Sample<List<double>> nextSequence()
        {
            var sample = uniformSequenceGenerator_.nextSequence();
            x_.weight = sample.weight;
            for (var i = 0; i < dimension_; i++)
            {
                x_.value[i] = ICD_.value(sample.value[i]);
            }

            return x_;
        }

        public Sample<List<double>> lastSequence() => x_;

        public int dimension() => dimension_;

        public IRNG factory(int dimensionality, ulong seed) => throw new NotSupportedException();

        #endregion
    }
}
