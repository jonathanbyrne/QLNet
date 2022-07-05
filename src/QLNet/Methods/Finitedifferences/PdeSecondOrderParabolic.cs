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

using QLNet.Math;
using QLNet.processes;

namespace QLNet.Methods.Finitedifferences
{
    public abstract class PdeSecondOrderParabolic
    {
        public abstract double diffusion(double t, double x);

        public abstract double discount(double t, double x);

        public abstract double drift(double t, double x);

        public abstract PdeSecondOrderParabolic factory(GeneralizedBlackScholesProcess process);

        public void generateOperator(double t, TransformedGrid tg, TridiagonalOperator L)
        {
            for (var i = 1; i < tg.size() - 1; i++)
            {
                var sigma = diffusion(t, tg.grid(i));
                var nu = drift(t, tg.grid(i));
                var r = discount(t, tg.grid(i));
                var sigma2 = sigma * sigma;

                var pd = -(sigma2 / tg.dxm(i) - nu) / tg.dx(i);
                var pu = -(sigma2 / tg.dxp(i) + nu) / tg.dx(i);
                var pm = sigma2 / (tg.dxm(i) * tg.dxp(i)) + r;
                L.setMidRow(i, pd, pm, pu);
            }
        }
    }
}
