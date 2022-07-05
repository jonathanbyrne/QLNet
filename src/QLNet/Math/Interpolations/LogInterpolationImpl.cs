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

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class LogInterpolationImpl<Interpolator> : Interpolation.templateImpl
        where Interpolator : IInterpolationFactory, new()
    {
        private Interpolation interpolation_;
        private List<double> logY_;

        public LogInterpolationImpl(List<double> xBegin, int size, List<double> yBegin)
            : this(xBegin, size, yBegin, FastActivator<Interpolator>.Create())
        {
        }

        public LogInterpolationImpl(List<double> xBegin, int size, List<double> yBegin, IInterpolationFactory factory)
            : base(xBegin, size, yBegin)
        {
            logY_ = new InitializedList<double>(size_);
            interpolation_ = factory.interpolate(xBegin_, size, logY_);
        }

        public override double derivative(double x) => value(x) * interpolation_.derivative(x, true);

        public override double primitive(double x) => throw new NotImplementedException("LogInterpolation primitive not implemented");

        public override double secondDerivative(double x) =>
            derivative(x) * interpolation_.derivative(x, true) +
            value(x) * interpolation_.secondDerivative(x, true);

        public override void update()
        {
            for (var i = 0; i < size_; ++i)
            {
                Utils.QL_REQUIRE(yBegin_[i] > 0.0, () => "invalid value (" + yBegin_[i] + ") at index " + i);
                logY_[i] = System.Math.Log(yBegin_[i]);
            }

            interpolation_.update();
        }

        public override double value(double x) => System.Math.Exp(interpolation_.value(x, true));
    }

    //! log-linear interpolation factory and traits

    //! %log-linear interpolation between discrete points

    //! log-cubic interpolation factory and traits

    //! %log-cubic interpolation between discrete points
}
