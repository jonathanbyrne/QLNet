﻿/*
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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.RandomNumbers;

namespace QLNet.Methods.montecarlo
{
    //! Generates random paths using a sequence generator
    /*! Generates random paths with drift(S,t) and variance(S,t)
        using a gaussian sequence generator

        \ingroup mcarlo

        \test the generated paths are checked against cached results
    */

    [PublicAPI]
    public class PathGenerator<GSG> : IPathGenerator<GSG> where GSG : IRNG
    {
        private BrownianBridge bb_;
        private bool brownianBridge_;
        private int dimension_;
        private GSG generator_;
        private Sample<IPath> next_;
        private StochasticProcess1D process_;
        private List<double> temp_;
        private TimeGrid timeGrid_;

        // constructors
        public PathGenerator(StochasticProcess process, double length, int timeSteps, GSG generator, bool brownianBridge)
        {
            brownianBridge_ = brownianBridge;
            generator_ = generator;
            dimension_ = generator_.dimension();
            timeGrid_ = new TimeGrid(length, timeSteps);
            process_ = process as StochasticProcess1D;
            next_ = new Sample<IPath>(new Path(timeGrid_), 1.0);
            temp_ = new InitializedList<double>(dimension_);
            bb_ = new BrownianBridge(timeGrid_);
            QLNet.Utils.QL_REQUIRE(dimension_ == timeSteps, () =>
                "sequence generator dimensionality (" + dimension_ + ") != timeSteps (" + timeSteps + ")");
        }

        public PathGenerator(StochasticProcess process, TimeGrid timeGrid, GSG generator, bool brownianBridge)
        {
            brownianBridge_ = brownianBridge;
            generator_ = generator;
            dimension_ = generator_.dimension();
            timeGrid_ = timeGrid;
            process_ = process as StochasticProcess1D;
            next_ = new Sample<IPath>(new Path(timeGrid_), 1.0);
            temp_ = new InitializedList<double>(dimension_);
            bb_ = new BrownianBridge(timeGrid_);

            QLNet.Utils.QL_REQUIRE(dimension_ == timeGrid_.size() - 1, () =>
                "sequence generator dimensionality (" + dimension_ + ") != timeSteps (" + (timeGrid_.size() - 1) + ")");
        }

        public Sample<IPath> antithetic() => next(true);

        public Sample<IPath> next() => next(false);

        private Sample<IPath> next(bool antithetic)
        {
            var sequence_ =
                antithetic
                    ? generator_.lastSequence()
                    : generator_.nextSequence();

            if (brownianBridge_)
            {
                bb_.transform(sequence_.value, temp_);
            }
            else
            {
                temp_ = new List<double>(sequence_.value);
            }

            next_.weight = sequence_.weight;

            var path = (Path)next_.value;
            path.setFront(process_.x0());

            for (var i = 1; i < path.length(); i++)
            {
                var t = timeGrid_[i - 1];
                var dt = timeGrid_.dt(i - 1);
                path[i] = process_.evolve(t, path[i - 1], dt,
                    antithetic
                        ? -temp_[i - 1]
                        : temp_[i - 1]);
            }

            return next_;
        }
    }
}
