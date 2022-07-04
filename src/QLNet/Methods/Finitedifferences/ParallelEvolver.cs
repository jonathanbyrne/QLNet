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
using QLNet.Patterns;
using System.Collections.Generic;

namespace QLNet.Methods.Finitedifferences
{
    /*! \brief Parallel evolver for multiple arrays

        This class takes the evolver class and creates a new class which evolves
        each of the evolvers in parallel.  Part of what this does is to take the
        types for each evolver class and then wrapper them so that they create
        new types which are sets of the old types.

        This class is intended to be run in situations where there are parallel
        differential equations such as with some convertible bond models.
    */
    /*! \ingroup findiff */

    [JetBrains.Annotations.PublicAPI] public class ParallelEvolver<Evolver> : IMixedScheme, ISchemeFactory where Evolver : IMixedScheme, ISchemeFactory, new()
    {
        private List<IMixedScheme> evolvers_;

        // required for generics
        public ParallelEvolver() { }
        public ParallelEvolver(List<IOperator> L, BoundaryConditionSet bcs)
        {
            evolvers_ = new List<IMixedScheme>(L.Count);
            for (var i = 0; i < L.Count; i++)
            {
                evolvers_.Add(FastActivator<Evolver>.Create().factory(L[i], bcs[i]));
            }
        }

        public void step(ref object o, double t, double theta = 1.0)
        {
            var a = (List<Vector>)o;
            for (var i = 0; i < evolvers_.Count; i++)
            {
                object temp = a[i];
                evolvers_[i].step(ref temp, t);
                a[i] = temp as Vector;
            }
        }

        public void setStep(double dt)
        {
            for (var i = 0; i < evolvers_.Count; i++)
            {
                evolvers_[i].setStep(dt);
            }
        }

        public IMixedScheme factory(object L, object bcs, object[] additionalFields = null) => new ParallelEvolver<Evolver>((List<IOperator>)L, (BoundaryConditionSet)bcs);
    }
}
