/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

namespace QLNet.Instruments
{
    //! Base class for options on multiple assets
    [PublicAPI]
    public class MultiAssetOption : Option
    {
        [PublicAPI]
        public class Engine : GenericEngine<Arguments, Results>
        {
        }

        public new class Results : Instrument.Results
        {
            public double? delta { get; set; }

            public double? dividendRho { get; set; }

            public double? gamma { get; set; }

            public double? rho { get; set; }

            public double? theta { get; set; }

            public double? vega { get; set; }

            public override void reset()
            {
                base.reset();
                delta = gamma = theta = vega = rho = dividendRho = null;
            }
        }

        // results
        protected double? delta_;
        protected double? dividendRho_;
        protected double? gamma_;
        protected double? rho_;
        protected double? theta_;
        protected double? vega_;

        public MultiAssetOption(Payoff payoff, Exercise exercise) : base(payoff, exercise)
        {
        }

        // greeks
        public double delta()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(delta_ != null, () => "delta not provided");
            return delta_.GetValueOrDefault();
        }

        public double dividendRho()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(dividendRho_ != null, () => "dividend rho not provided");
            return dividendRho_.GetValueOrDefault();
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);

            var results = r as Results;
            QLNet.Utils.QL_REQUIRE(results != null, () => "no greeks returned from pricing engine");

            delta_ = results.delta;
            gamma_ = results.gamma;
            theta_ = results.theta;
            vega_ = results.vega;
            rho_ = results.rho;
            dividendRho_ = results.dividendRho;
        }

        public double gamma()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(gamma_ != null, () => "gamma not provided");
            return gamma_.GetValueOrDefault();
        }

        // Instrument interface
        public override bool isExpired() => new simple_event(exercise_.lastDate()).hasOccurred();

        public double rho()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(rho_ != null, () => "rho not provided");
            return rho_.GetValueOrDefault();
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            var arguments = args as Arguments;
            QLNet.Utils.QL_REQUIRE(arguments != null, () => "wrong argument ExerciseType");

            arguments.payoff = payoff_;
            arguments.exercise = exercise_;
        }

        public double theta()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(theta_ != null, () => "theta not provided");
            return theta_.GetValueOrDefault();
        }

        public double vega()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(vega_ != null, () => "vega not provided");
            return vega_.GetValueOrDefault();
        }

        protected override void setupExpired()
        {
            NPV_ = delta_ = gamma_ = theta_ = vega_ = rho_ = dividendRho_ = 0.0;
        }
    }
}
