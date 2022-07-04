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

using QLNet.Methods.Finitedifferences;
using QLNet.Methods.Finitedifferences.Operators;
using QLNet.Methods.Finitedifferences.Schemes;
using QLNet.Methods.Finitedifferences.StepConditions;
using QLNet.Methods.Finitedifferences.Utilities;
using System;

namespace QLNet.Methods.Finitedifferences.Solvers
{
    [JetBrains.Annotations.PublicAPI] public class FdmBackwardSolver
    {
        public FdmBackwardSolver(FdmLinearOpComposite map,
                                 FdmBoundaryConditionSet bcSet,
                                 FdmStepConditionComposite condition,
                                 FdmSchemeDesc schemeDesc)
        {
            map_ = map;
            bcSet_ = bcSet;
            condition_ = condition;
            schemeDesc_ = schemeDesc;
        }

        public void rollback(ref object a,
                             double from, double to,
                             int steps, int dampingSteps)
        {
            var deltaT = from - to;
            var allSteps = steps + dampingSteps;
            var dampingTo = from - deltaT * dampingSteps / allSteps;

            if (dampingSteps > 0
                && schemeDesc_.type != FdmSchemeDesc.FdmSchemeType.ImplicitEulerType)
            {
                var implicitEvolver = new ImplicitEulerScheme(map_, bcSet_);
                var dampingModel
                   = new FiniteDifferenceModel<ImplicitEulerScheme>(implicitEvolver, condition_.stoppingTimes());

                dampingModel.rollback(ref a, from, dampingTo,
                                      dampingSteps, condition_);
            }

            switch (schemeDesc_.type)
            {
                case FdmSchemeDesc.FdmSchemeType.HundsdorferType:
                    {
                        var hsEvolver = new HundsdorferScheme(schemeDesc_.theta, schemeDesc_.mu,
                                                                            map_, bcSet_);
                        var
                        hsModel = new FiniteDifferenceModel<HundsdorferScheme>(hsEvolver, condition_.stoppingTimes());
                        hsModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.DouglasType:
                    {
                        var dsEvolver = new DouglasScheme(schemeDesc_.theta, map_, bcSet_);
                        var
                        dsModel = new FiniteDifferenceModel<DouglasScheme>(dsEvolver, condition_.stoppingTimes());
                        dsModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.CrankNicolsonType:
                    {
                        var cnEvolver = new CrankNicolsonScheme(schemeDesc_.theta, map_, bcSet_);
                        var
                        cnModel = new FiniteDifferenceModel<CrankNicolsonScheme>(cnEvolver, condition_.stoppingTimes());
                        cnModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.CraigSneydType:
                    {
                        var csEvolver = new CraigSneydScheme(schemeDesc_.theta, schemeDesc_.mu,
                                                                          map_, bcSet_);
                        var
                        csModel = new FiniteDifferenceModel<CraigSneydScheme>(csEvolver, condition_.stoppingTimes());
                        csModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.ModifiedCraigSneydType:
                    {
                        var csEvolver = new ModifiedCraigSneydScheme(schemeDesc_.theta,
                                                                                          schemeDesc_.mu,
                                                                                          map_, bcSet_);
                        var
                        mcsModel = new FiniteDifferenceModel<ModifiedCraigSneydScheme>(csEvolver, condition_.stoppingTimes());
                        mcsModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.ImplicitEulerType:
                    {
                        var implicitEvolver = new ImplicitEulerScheme(map_, bcSet_);
                        var
                        implicitModel = new FiniteDifferenceModel<ImplicitEulerScheme>(implicitEvolver, condition_.stoppingTimes());
                        implicitModel.rollback(ref a, from, to, allSteps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.ExplicitEulerType:
                    {
                        var explicitEvolver = new ExplicitEulerScheme(map_, bcSet_);
                        var
                        explicitModel = new FiniteDifferenceModel<ExplicitEulerScheme>(explicitEvolver, condition_.stoppingTimes());
                        explicitModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.MethodOfLinesType:
                    {
                        var methodOfLines = new MethodOfLinesScheme(schemeDesc_.theta, schemeDesc_.mu, map_, bcSet_);
                        var
                        molModel = new FiniteDifferenceModel<MethodOfLinesScheme>(methodOfLines, condition_.stoppingTimes());
                        molModel.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                case FdmSchemeDesc.FdmSchemeType.TrBDF2Type:
                    {
                        var trDesc = new FdmSchemeDesc().CraigSneyd();
                        var hsEvolver = new CraigSneydScheme(trDesc.theta, trDesc.mu, map_, bcSet_);

                        var trBDF2 = new TrBDF2Scheme<CraigSneydScheme>(
                           schemeDesc_.theta, map_, hsEvolver, bcSet_, schemeDesc_.mu);

                        var
                        trBDF2Model = new FiniteDifferenceModel<TrBDF2Scheme<CraigSneydScheme>>(trBDF2, condition_.stoppingTimes());
                        trBDF2Model.rollback(ref a, dampingTo, to, steps, condition_);
                    }
                    break;
                default:
                    Utils.QL_FAIL("Unknown scheme ExerciseType");
                    break;
            }
        }

        protected FdmLinearOpComposite map_;
        protected FdmBoundaryConditionSet bcSet_;
        protected FdmStepConditionComposite condition_;
        protected FdmSchemeDesc schemeDesc_;
    }
}
