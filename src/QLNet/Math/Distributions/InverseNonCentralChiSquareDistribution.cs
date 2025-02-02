﻿using System;
using JetBrains.Annotations;
using QLNet.Math.Solvers1d;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class InverseNonCentralChiSquareDistribution
    {
        private class IncChiQuareFinder : ISolver1d
        {
            private readonly Func<double, double> g_;
            private readonly double y_;

            public IncChiQuareFinder(double y, Func<double, double> g)
            {
                y_ = y;
                g_ = g;
            }

            public override double value(double x) => g_(x) - y_;
        }

        private double accuracy_;
        private double guess_;
        private int maxEvaluations_;
        private NonCentralChiSquareDistribution nonCentralDist_;

        public InverseNonCentralChiSquareDistribution(double df, double ncp,
            int maxEvaluations,
            double accuracy)
        {
            nonCentralDist_ = new NonCentralChiSquareDistribution(df, ncp);
            guess_ = df + ncp;
            maxEvaluations_ = maxEvaluations;
            accuracy_ = accuracy;
        }

        public InverseNonCentralChiSquareDistribution(double df, double ncp, int maxEvaluations)
            : this(df, ncp, maxEvaluations, 1e-8)
        {
        }

        public InverseNonCentralChiSquareDistribution(double df, double ncp)
            : this(df, ncp, 10, 1e-8)
        {
        }

        public double value(double x)
        {
            // first find the right side of the interval
            var upper = guess_;
            var evaluations = maxEvaluations_;
            while (nonCentralDist_.value(upper) < x && evaluations > 0)
            {
                upper *= 2.0;
                --evaluations;
            }

            // use a brent solver for the rest
            var solver = new Brent();
            solver.setMaxEvaluations(evaluations);
            return solver.solve(new IncChiQuareFinder(x, nonCentralDist_.value),
                accuracy_, 0.75 * upper, evaluations == maxEvaluations_ ? 0.0 : 0.5 * upper,
                upper);
        }
    }
}
