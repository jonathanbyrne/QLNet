﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Methods.montecarlo;
using QLNet.Models.MarketModels.BrownianGenerators;

namespace QLNet.Math.RandomNumbers
{
    // Interface class to map the functionality of SobolBrownianGenerator
    // to the "conventional" sequence generator interface
    [PublicAPI]
    public class SobolBrownianBridgeRsg : IRNG
    {
        private int factors_, steps_, dim_;
        private SobolBrownianGenerator gen_;
        private Sample<List<double>> seq_;

        public SobolBrownianBridgeRsg(int factors, int steps,
            SobolBrownianGenerator.Ordering ordering = SobolBrownianGenerator.Ordering.Diagonal,
            ulong seed = 0,
            SobolRsg.DirectionIntegers directionIntegers = SobolRsg.DirectionIntegers.JoeKuoD7)
        {
            factors_ = factors;
            steps_ = steps;
            dim_ = factors * steps;
            seq_ = new Sample<List<double>>(new InitializedList<double>(factors * steps), 1.0);
            gen_ = new SobolBrownianGenerator(factors, steps, ordering, seed, directionIntegers);
        }

        public int dimension() => dim_;

        public IRNG factory(int dimensionality, ulong seed) => throw new NotImplementedException();

        public Sample<List<double>> lastSequence() => seq_;

        public Sample<List<double>> nextSequence()
        {
            gen_.nextPath();
            List<double> output = new InitializedList<double>(factors_);
            for (var i = 0; i < steps_; ++i)
            {
                gen_.nextStep(output);
                for (var j = 0; j < output.Count; j++)
                {
                    seq_.value[j + i * factors_] = output[j];
                }
            }

            return seq_;
        }
    }
}
