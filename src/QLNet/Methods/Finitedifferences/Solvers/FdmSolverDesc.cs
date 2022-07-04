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
        private FdmMesher _mesher;
        private FdmBoundaryConditionSet _bcSet;
        private FdmStepConditionComposite _condition;
        private FdmInnerValueCalculator _calculator;
        private double _maturity;
        private int _timeSteps;
        private int _dampingSteps;

        public FdmMesher mesher
        {
            get => _mesher;
            set => _mesher = value;
        }

        public FdmBoundaryConditionSet bcSet
        {
            get => _bcSet;
            set => _bcSet = value;
        }

        public FdmStepConditionComposite condition
        {
            get => _condition;
            set => _condition = value;
        }

        public FdmInnerValueCalculator calculator
        {
            get => _calculator;
            set => _calculator = value;
        }

        public double maturity
        {
            get => _maturity;
            set => _maturity = value;
        }

        public int timeSteps
        {
            get => _timeSteps;
            set => _timeSteps = value;
        }

        public int dampingSteps
        {
            get => _dampingSteps;
            set => _dampingSteps = value;
        }
    }
}
