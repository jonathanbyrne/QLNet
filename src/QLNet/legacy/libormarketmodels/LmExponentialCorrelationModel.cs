/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Math.matrixutilities;
using QLNet.Math.Optimization;
using QLNet.Models;

namespace QLNet.legacy.libormarketmodels
{
    //! exponential correlation model
    /*! This class describes a exponential correlation model

        References:

        Damiano Brigo, Fabio Mercurio, Massimo Morini, 2003,
        Different Covariance Parameterizations of Libor Market Model and Joint
        Caps/Swaptions Calibration,
        (<http://www.business.uts.edu.au/qfrc/conferences/qmf2001/Brigo_D.pdf>)
    */

    [PublicAPI]
    public class LmExponentialCorrelationModel : LmCorrelationModel
    {
        private Matrix corrMatrix_, pseudoSqrt_;

        public LmExponentialCorrelationModel(int size, double rho)
            : base(size, 1)
        {
            corrMatrix_ = new Matrix(size, size);
            pseudoSqrt_ = new Matrix(size, size);
            arguments_[0] = new ConstantParameter(rho, new PositiveConstraint());
            generateArguments();
        }

        public override Matrix correlation(double t, Vector x = null)
        {
            var tmp = new Matrix(corrMatrix_);
            return tmp;
        }

        public override double correlation(int i, int j, double t, Vector x = null) => corrMatrix_[i, j];

        public override bool isTimeIndependent() => true;

        public override Matrix pseudoSqrt(double t, Vector x = null)
        {
            var tmp = new Matrix(pseudoSqrt_);
            return tmp;
        }

        protected override void generateArguments()
        {
            var rho = arguments_[0].value(0.0);

            for (var i = 0; i < size_; ++i)
            {
                for (var j = i; j < size_; ++j)
                {
                    corrMatrix_[i, j] = corrMatrix_[j, i] = System.Math.Exp(-rho * System.Math.Abs(i - (double)j));
                }
            }

            pseudoSqrt_ = MatrixUtilitites.pseudoSqrt(corrMatrix_, MatrixUtilitites.SalvagingAlgorithm.Spectral);
        }
    }
}
