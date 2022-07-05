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
using QLNet.Math.RandomNumbers;

namespace QLNet.Methods.montecarlo
{
    //! Generates a multipath from a random number generator.
    /*! RSG is a sample generator which returns a random sequence.

        \test the generated paths are checked against cached results
    */

    [PublicAPI]
    public class MultiPathGenerator<GSG> : IPathGenerator<GSG> where GSG : IRNG
    {
        private bool brownianBridge_;
        private GSG generator_;
        private Sample<IPath> next_;
        private StochasticProcess process_;

        public MultiPathGenerator(StochasticProcess process, TimeGrid times, GSG generator, bool brownianBridge)
        {
            brownianBridge_ = brownianBridge;
            process_ = process;
            generator_ = generator;
            next_ = new Sample<IPath>(new MultiPath(process.size(), times), 1.0);

            QLNet.Utils.QL_REQUIRE(generator_.dimension() == process.factors() * (times.size() - 1), () =>
                "dimension (" + generator_.dimension()
                              + ") is not equal to ("
                              + process.factors() + " * " + (times.size() - 1)
                              + ") the number of factors "
                              + "times the number of time steps");
            QLNet.Utils.QL_REQUIRE(times.size() > 1, () => "no times given");
        }

        public Sample<IPath> antithetic() => next(true);

        public Sample<IPath> next() => next(false);

        private Sample<IPath> next(bool antithetic)
        {
            if (brownianBridge_)
            {
                QLNet.Utils.QL_FAIL("Brownian bridge not supported");
                return null;
            }

            var sequence_ =
                antithetic
                    ? generator_.lastSequence()
                    : generator_.nextSequence();

            var m = process_.size();
            var n = process_.factors();

            var path = (MultiPath)next_.value;

            var asset = process_.initialValues();
            for (var j = 0; j < m; j++)
            {
                path[j].setFront(asset[j]);
            }

            Vector temp;
            next_.weight = sequence_.weight;

            var timeGrid = path[0].timeGrid();
            double t, dt;
            for (var i = 1; i < path.pathSize(); i++)
            {
                var offset = (i - 1) * n;
                t = timeGrid[i - 1];
                dt = timeGrid.dt(i - 1);
                if (antithetic)
                {
                    temp = -1 * new Vector(sequence_.value.GetRange(offset, n));
                }
                else
                {
                    temp = new Vector(sequence_.value.GetRange(offset, n));
                }

                asset = process_.evolve(t, asset, dt, temp);
                for (var j = 0; j < m; j++)
                {
                    path[j][i] = asset[j];
                }
            }

            return next_;
        }
    }
}
