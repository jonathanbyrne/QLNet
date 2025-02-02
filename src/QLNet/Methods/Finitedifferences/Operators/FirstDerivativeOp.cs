﻿/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.Methods.Finitedifferences.Meshers;

namespace QLNet.Methods.Finitedifferences.Operators
{
    [PublicAPI]
    public class FirstDerivativeOp : TripleBandLinearOp
    {
        public FirstDerivativeOp(FirstDerivativeOp rhs)
            : base(rhs.direction_, rhs.mesher_)
        {
            lower_ = rhs.lower_;
            diag_ = rhs.diag_;
            upper_ = rhs.upper_;
        }

        public FirstDerivativeOp(int direction, FdmMesher mesher)
            : base(direction, mesher)
        {
            var layout = mesher.layout();
            var endIter = layout.end();

            for (var iter = layout.begin(); iter != endIter; ++iter)
            {
                var i = iter.index();
                var hm = mesher.dminus(iter, direction_);
                var hp = mesher.dplus(iter, direction_);

                var zetam1 = hm * (hm + hp);
                var zeta0 = hm * hp;
                var zetap1 = hp * (hm + hp);

                if (iter.coordinates()[direction_] == 0)
                {
                    //upwinding scheme
                    lower_[i] = 0.0;
                    diag_[i] = -(upper_[i] = 1 / hp.Value);
                }
                else if (iter.coordinates()[direction_]
                         == layout.dim()[direction] - 1)
                {
                    // downwinding scheme
                    lower_[i] = -(diag_[i] = 1 / hm.Value);
                    upper_[i] = 0.0;
                }
                else
                {
                    lower_[i] = -hp.Value / zetam1.Value;
                    diag_[i] = (hp.Value - hm.Value) / zeta0.Value;
                    upper_[i] = hm.Value / zetap1.Value;
                }
            }
        }
    }
}
