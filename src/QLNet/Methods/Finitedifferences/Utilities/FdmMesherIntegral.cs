/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Methods.Finitedifferences.Meshers;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    [PublicAPI]
    public class FdmMesherIntegral
    {
        protected Func<Vector, Vector, double> integrator1d_;
        protected List<Fdm1dMesher> meshers_;

        public FdmMesherIntegral(
            FdmMesherComposite mesher,
            Func<Vector, Vector, double> integrator1d)
        {
            meshers_ = new List<Fdm1dMesher>(mesher.getFdm1dMeshers());
            integrator1d_ = integrator1d;
        }

        public double integrate(Vector f)
        {
            var x = new Vector(meshers_.Last().locations());

            if (meshers_.Count == 1)
            {
                return integrator1d_(x, f);
            }

            var subMesher =
                new FdmMesherComposite(
                    new List<Fdm1dMesher>(meshers_.GetRange(0, meshers_.Count - 1)));

            var subMesherIntegral = new FdmMesherIntegral(subMesher, integrator1d_);
            var subSize = subMesher.layout().size();

            Vector g = new Vector(x.size()), fSub = new Vector(subSize);

            for (var i = 0; i < x.size(); ++i)
            {
                f.copy(i * subSize,
                    (i + 1) * subSize, 0, fSub);

                g[i] = subMesherIntegral.integrate(fSub);
            }

            return integrator1d_(x, g);
        }
    }
}
