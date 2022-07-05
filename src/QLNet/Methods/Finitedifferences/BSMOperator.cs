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

using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Processes;

namespace QLNet.Methods.Finitedifferences
{
    //! Black-Scholes-Merton differential operator
    /*! \ingroup findiff */
    [PublicAPI]
    public class BSMOperator : TridiagonalOperator
    {
        public BSMOperator()
        {
        }

        public BSMOperator(int size, double dx, double r, double q, double sigma) : base(size)
        {
            var sigma2 = sigma * sigma;
            var nu = r - q - sigma2 / 2;
            var pd = -(sigma2 / dx - nu) / (2 * dx);
            var pu = -(sigma2 / dx + nu) / (2 * dx);
            var pm = sigma2 / (dx * dx) + r;
            setMidRows(pd, pm, pu);
        }

        public BSMOperator(Vector grid, GeneralizedBlackScholesProcess process, double residualTime) : base(grid.size())
        {
            var logGrid = new LogGrid(grid);
            var cc = new PdeConstantCoeff<PdeBSM>(process, residualTime, process.stateVariable().link.value());
            cc.generateOperator(residualTime, logGrid, this);
        }
    }
}
