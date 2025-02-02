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

using JetBrains.Annotations;
using QLNet.Processes;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public class PdeBSM : PdeSecondOrderParabolic
    {
        private GeneralizedBlackScholesProcess process_;

        public PdeBSM()
        {
        } // required for generics

        public PdeBSM(GeneralizedBlackScholesProcess process)
        {
            process_ = process;
        }

        public override double diffusion(double t, double x) => process_.diffusion(t, x);

        public override double discount(double t, double x)
        {
            if (System.Math.Abs(t) < 1e-8)
            {
                t = 0;
            }

            return process_.riskFreeRate().link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency, true).rate();
        }

        public override double drift(double t, double x) => process_.drift(t, x);

        public override PdeSecondOrderParabolic factory(GeneralizedBlackScholesProcess process) => new PdeBSM(process);
    }
}
