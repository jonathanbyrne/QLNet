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

using QLNet.Methods.Finitedifferences.Meshers;
using QLNet.Methods.Finitedifferences.StepConditions;
using QLNet.Methods.Finitedifferences.Utilities;

namespace QLNet.Methods.Finitedifferences.Solvers
{
    public struct FdmSolverDesc
    {
        public FdmBoundaryConditionSet bcSet { get; set; }

        public FdmInnerValueCalculator calculator { get; set; }

        public FdmStepConditionComposite condition { get; set; }

        public int dampingSteps { get; set; }

        public double maturity { get; set; }

        public FdmMesher mesher { get; set; }

        public int timeSteps { get; set; }
    }
}
