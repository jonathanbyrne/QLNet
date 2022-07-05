/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)

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

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class TrinomialTree : Tree<TrinomialTree>
    {
        public enum Branches
        {
            branches = 3
        }

        /* Branching scheme for a trinomial node.  Each node has three
           descendants, with the middle branch linked to the node
           which is closest to the expectation of the variable. */
        private class Branching
        {
            private readonly List<int> k_;
            private int kMin_, jMin_, kMax_, jMax_;
            private readonly List<List<double>> probs_;

            public Branching()
            {
                k_ = new List<int>();
                probs_ = new InitializedList<List<double>>(3);
                kMin_ = int.MaxValue;
                jMin_ = int.MaxValue;
                kMax_ = int.MinValue;
                jMax_ = int.MinValue;
            }

            public void add
                (int k, double p1, double p2, double p3)
            {
                // store
                k_.Add(k);
                probs_[0].Add(p1);
                probs_[1].Add(p2);
                probs_[2].Add(p3);
                // maintain invariants
                kMin_ = System.Math.Min(kMin_, k);
                jMin_ = kMin_ - 1;
                kMax_ = System.Math.Max(kMax_, k);
                jMax_ = kMax_ + 1;
            }

            public int descendant(int index, int branch) => k_[index] - jMin_ - 1 + branch;

            public int jMax() => jMax_;

            public int jMin() => jMin_;

            public double probability(int index, int branch) => probs_[branch][index];

            public int size() => jMax_ - jMin_ + 1;
        }

        protected List<double> dx_;
        protected TimeGrid timeGrid_;
        protected double x0_;
        private List<Branching> branchings_;

        public TrinomialTree(StochasticProcess1D process,
            TimeGrid timeGrid)
            : this(process, timeGrid, false)
        {
        }

        public TrinomialTree(StochasticProcess1D process,
            TimeGrid timeGrid,
            bool isPositive /*= false*/)
            : base(timeGrid.size())
        {
            branchings_ = new List<Branching>();
            dx_ = new InitializedList<double>(1);
            timeGrid_ = timeGrid;
            x0_ = process.x0();

            var nTimeSteps = timeGrid.size() - 1;
            var jMin = 0;
            var jMax = 0;

            for (var i = 0; i < nTimeSteps; i++)
            {
                var t = timeGrid[i];
                var dt = timeGrid.dt(i);

                //Variance must be independent of x
                var v2 = process.variance(t, 0.0, dt);
                var v = System.Math.Sqrt(v2);
                dx_.Add(v * System.Math.Sqrt(3.0));

                var branching = new Branching();
                for (var j = jMin; j <= jMax; j++)
                {
                    var x = x0_ + j * dx_[i];
                    var m = process.expectation(t, x, dt);
                    var temp = (int)System.Math.Floor((m - x0_) / dx_[i + 1] + 0.5);

                    if (isPositive)
                    {
                        while (x0_ + (temp - 1) * dx_[i + 1] <= 0)
                        {
                            temp++;
                        }
                    }

                    var e = m - (x0_ + temp * dx_[i + 1]);
                    var e2 = e * e;
                    var e3 = e * System.Math.Sqrt(3.0);

                    var p1 = (1.0 + e2 / v2 - e3 / v) / 6.0;
                    var p2 = (2.0 - e2 / v2) / 3.0;
                    var p3 = (1.0 + e2 / v2 + e3 / v) / 6.0;

                    branching.add(temp, p1, p2, p3);
                }

                branchings_.Add(branching);

                jMin = branching.jMin();
                jMax = branching.jMax();
            }
        }

        public int descendant(int i, int index, int branch) => branchings_[i].descendant(index, branch);

        public double dx(int i) => dx_[i];

        public double probability(int i, int index, int branch) => branchings_[i].probability(index, branch);

        public int size(int i) => i == 0 ? 1 : branchings_[i - 1].size();

        public TimeGrid timeGrid() => timeGrid_;

        public double underlying(int i, int index)
        {
            if (i == 0)
            {
                return x0_;
            }

            return x0_ + (branchings_[i - 1].jMin() +
                          (double)index) * dx(i);
        }
    }
}
