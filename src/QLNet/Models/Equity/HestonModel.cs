//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Optimization;
using QLNet.processes;

namespace QLNet.Models.Equity
{
    //! Heston model for the stochastic volatility of an asset
    /*! References:

        Heston, Steven L., 1993. A Closed-Form Solution for Options
        with Stochastic Volatility with Applications to Bond and
        Currency Options.  The review of Financial Studies, Volume 6,
        Issue 2, 327-343.

        \test calibration is tested against known good values.
    */
    [PublicAPI]
    public class HestonModel : CalibratedModel
    {
        [PublicAPI]
        public class FellerConstraint : Constraint
        {
            private class Impl : IConstraint
            {
                public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), double.MinValue);

                public bool test(Vector param)
                {
                    var theta = param[0];
                    var kappa = param[1];
                    var sigma = param[2];

                    return sigma >= 0.0 && sigma * sigma < 2.0 * kappa * theta;
                }

                public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);
            }

            public FellerConstraint()
                : base(new Impl())
            {
            }
        }

        protected HestonProcess process_;

        public HestonModel(HestonProcess process)
            : base(5)
        {
            process_ = process;

            arguments_[0] = new ConstantParameter(process.theta(), new PositiveConstraint());
            arguments_[1] = new ConstantParameter(process.kappa(), new PositiveConstraint());
            arguments_[2] = new ConstantParameter(process.sigma(), new PositiveConstraint());
            arguments_[3] = new ConstantParameter(process.rho(), new BoundaryConstraint(-1.0, 1.0));
            arguments_[4] = new ConstantParameter(process.v0(), new PositiveConstraint());
            generateArguments();

            process_.riskFreeRate().registerWith(update);
            process_.dividendYield().registerWith(update);
            process_.s0().registerWith(update);
        }

        // variance mean reversion speed
        public double kappa() => arguments_[1].value(0.0);

        // underlying process
        public HestonProcess process() => process_;

        // correlation
        public double rho() => arguments_[3].value(0.0);

        // volatility of the volatility
        public double sigma() => arguments_[2].value(0.0);

        // variance mean version level
        public double theta() => arguments_[0].value(0.0);

        // spot variance
        public double v0() => arguments_[4].value(0.0);

        protected override void generateArguments()
        {
            process_ = new HestonProcess(process_.riskFreeRate(),
                process_.dividendYield(),
                process_.s0(),
                v0(), kappa(), theta(),
                sigma(), rho());
        }
    }
}
