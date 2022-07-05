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

namespace QLNet.Instruments
{
    //! Base class for options on a single asset
    [PublicAPI]
    public class OneAssetOption : Option
    {
        [PublicAPI]
        public class Engine : GenericEngine<Arguments, Results>
        {
        }

        //! %Results from single-asset option calculation
        public new class Results : Instrument.Results
        {
            public double? delta { get; set; }

            public double? deltaForward { get; set; }

            public double? dividendRho { get; set; }

            public double? elasticity { get; set; }

            public double? gamma { get; set; }

            public double? itmCashProbability { get; set; }

            public double? rho { get; set; }

            public double? strikeSensitivity { get; set; }

            public double? theta { get; set; }

            public double? thetaPerDay { get; set; }

            public double? vega { get; set; }

            public override void reset()
            {
                base.reset();
                delta = gamma = theta = vega = rho = dividendRho = null;
                itmCashProbability = deltaForward = elasticity = thetaPerDay = strikeSensitivity = null;
            }
        }

        // results
        protected double? delta_,
            deltaForward_,
            elasticity_,
            gamma_,
            theta_,
            thetaPerDay_,
            vega_,
            rho_,
            dividendRho_,
            strikeSensitivity_,
            itmCashProbability_;

        public OneAssetOption(Payoff payoff, Exercise exercise) : base(payoff, exercise)
        {
        }

        public double delta()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(delta_ != null, () => "delta not provided");
            return delta_.GetValueOrDefault();
        }

        public double deltaForward()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(deltaForward_ != null, () => "forward delta not provided");
            return deltaForward_.GetValueOrDefault();
        }

        public double dividendRho()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(dividendRho_ != null, () => "dividend rho not provided");
            return dividendRho_.GetValueOrDefault();
        }

        public double elasticity()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(elasticity_ != null, () => "elasticity not provided");
            return elasticity_.GetValueOrDefault();
        }

        public override void fetchResults(IPricingEngineResults r)
        {
            base.fetchResults(r);

            var results = r as Results;
            QLNet.Utils.QL_REQUIRE(results != null, () => "no greeks returned from pricing engine");
            /* no check on null values - just copy.
               this allows:
               a) to decide in derived options what to do when null
               results are returned (throw? numerical calculation?)
               b) to implement slim engines which only calculate the
               value---of course care must be taken not to call
               the greeks methods when using these.
            */
            delta_ = results.delta;
            gamma_ = results.gamma;
            theta_ = results.theta;
            vega_ = results.vega;
            rho_ = results.rho;
            dividendRho_ = results.dividendRho;

            /* no check on null values - just copy.
               this allows:
               a) to decide in derived options what to do when null
               results are returned (throw? numerical calculation?)
               b) to implement slim engines which only calculate the
               value---of course care must be taken not to call
               the greeks methods when using these.
            */
            deltaForward_ = results.deltaForward;
            elasticity_ = results.elasticity;
            thetaPerDay_ = results.thetaPerDay;
            strikeSensitivity_ = results.strikeSensitivity;
            itmCashProbability_ = results.itmCashProbability;
        }

        public double gamma()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(gamma_ != null, () => "gamma not provided");
            return gamma_.GetValueOrDefault();
        }

        public override bool isExpired() => new simple_event(exercise_.lastDate()).hasOccurred();

        public double itmCashProbability()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(itmCashProbability_ != null, () => "in-the-money cash probability not provided");
            return itmCashProbability_.GetValueOrDefault();
        }

        public double rho()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(rho_ != null, () => "rho not provided");
            return rho_.GetValueOrDefault();
        }

        public double strikeSensitivity()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(strikeSensitivity_ != null, () => "strike sensitivity not provided");
            return strikeSensitivity_.GetValueOrDefault();
        }

        public double theta()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(theta_ != null, () => "theta not provided");
            return theta_.GetValueOrDefault();
        }

        public double thetaPerDay()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(thetaPerDay_ != null, () => "theta per-day not provided");
            return thetaPerDay_.GetValueOrDefault();
        }

        public double vega()
        {
            calculate();
            QLNet.Utils.QL_REQUIRE(vega_ != null, () => "vega not provided");
            return vega_.GetValueOrDefault();
        }

        protected override void setupExpired()
        {
            base.setupExpired();
            delta_ = deltaForward_ = elasticity_ = gamma_ = theta_ = thetaPerDay_ = vega_ = rho_ = dividendRho_ =
                strikeSensitivity_ = itmCashProbability_ = 0.0;
        }
    }
}
