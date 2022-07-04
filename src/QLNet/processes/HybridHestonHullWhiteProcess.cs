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
using QLNet.Math;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.Time;
using System;

namespace QLNet.processes
{
    //! Hybrid Heston Hull-White stochastic process
    /*! This class implements a three factor Heston Hull-White model

        \bug This class was not tested enough to guarantee
             its functionality... work in progress

        \ingroup processes
    */
    [JetBrains.Annotations.PublicAPI] public class HybridHestonHullWhiteProcess : StochasticProcess
    {
        public enum Discretization { Euler, BSMHullWhite }

        public HybridHestonHullWhiteProcess(HestonProcess hestonProcess,
                                            HullWhiteForwardProcess hullWhiteProcess,
                                            double corrEquityShortRate,
                                            Discretization discretization = Discretization.BSMHullWhite)
        {
            hestonProcess_ = hestonProcess;
            hullWhiteProcess_ = hullWhiteProcess;
            hullWhiteModel_ = new HullWhite(hestonProcess.riskFreeRate(),
                                            hullWhiteProcess.a(),
                                            hullWhiteProcess.sigma());
            corrEquityShortRate_ = corrEquityShortRate;
            disc_ = discretization;
            maxRho_ = System.Math.Sqrt(1 - hestonProcess.rho() * hestonProcess.rho())
                      - System.Math.Sqrt(Const.QL_EPSILON) /* reserve for rounding errors */;

            T_ = hullWhiteProcess.getForwardMeasureTime();
            endDiscount_ = hestonProcess.riskFreeRate().link.discount(T_);

            Utils.QL_REQUIRE(corrEquityShortRate * corrEquityShortRate
                             + hestonProcess.rho() * hestonProcess.rho() <= 1.0, () =>
                             "correlation matrix is not positive definite");

            Utils.QL_REQUIRE(hullWhiteProcess.sigma() > 0.0, () =>
                             "positive vol of Hull White process is required");
        }

        public override int size() => 3;

        public override Vector initialValues()
        {
            var retVal = new Vector(3);
            retVal[0] = hestonProcess_.s0().link.value();
            retVal[1] = hestonProcess_.v0();
            retVal[2] = hullWhiteProcess_.x0();

            return retVal;
        }
        public override Vector drift(double t, Vector x)
        {
            Vector retVal = new Vector(3), x0 = new Vector(2);

            x0[0] = x[0]; x0[1] = x[1];
            var y0 = hestonProcess_.drift(t, x0);

            retVal[0] = y0[0]; retVal[1] = y0[1];
            retVal[2] = hullWhiteProcess_.drift(t, x[2]);

            return retVal;
        }
        public override Matrix diffusion(double t, Vector x)
        {
            var retVal = new Matrix(3, 3);

            var xt = new Vector(2);
            xt[0] = x[0];
            xt[1] = x[1];
            var m = hestonProcess_.diffusion(t, xt);
            retVal[0, 0] = m[0, 0]; retVal[0, 1] = 0.0; retVal[0, 2] = 0.0;
            retVal[1, 0] = m[1, 0]; retVal[1, 1] = m[1, 1]; retVal[1, 2] = 0.0;

            var sigma = hullWhiteProcess_.sigma();
            retVal[2, 0] = corrEquityShortRate_ * sigma;
            retVal[2, 1] = -retVal[2, 0] * retVal[1, 0] / retVal[1, 1];
            retVal[2, 2] = System.Math.Sqrt(sigma * sigma - retVal[2, 1] * retVal[2, 1]
                                     - retVal[2, 0] * retVal[2, 0]);

            return retVal;
        }
        public override Vector apply(Vector x0, Vector dx)
        {
            Vector retVal = new Vector(3), xt = new Vector(2), dxt = new Vector(2);

            xt[0] = x0[0]; xt[1] = x0[1];
            dxt[0] = dx[0]; dxt[1] = dx[1];

            var yt = hestonProcess_.apply(xt, dxt);

            retVal[0] = yt[0]; retVal[1] = yt[1];
            retVal[2] = hullWhiteProcess_.apply(x0[2], dx[2]);

            return retVal;
        }

        public override Vector evolve(double t0, Vector x0, double dt, Vector dw)
        {
            var r = x0[2];
            var a = hullWhiteProcess_.a();
            var sigma = hullWhiteProcess_.sigma();
            var rho = corrEquityShortRate_;
            var xi = hestonProcess_.rho();
            var eta = x0[1] > 0.0 ? System.Math.Sqrt(x0[1]) : 0.0;
            var s = t0;
            var t = t0 + dt;
            var T = T_;
            var dy = hestonProcess_.dividendYield().link.forwardRate(s, t, Compounding.Continuous, Frequency.NoFrequency).value();

            var df = System.Math.Log(hestonProcess_.riskFreeRate().link.discount(t)
                                     / hestonProcess_.riskFreeRate().link.discount(s));

            var eaT = System.Math.Exp(-a * T);
            var eat = System.Math.Exp(-a * t);
            var eas = System.Math.Exp(-a * s);
            var iat = 1.0 / eat;
            var ias = 1.0 / eas;

            var m1 = -(dy + 0.5 * eta * eta) * dt - df;

            var m2 = -rho * sigma * eta / a * (dt - 1 / a * eaT * (iat - ias));

            var m3 = (r - hullWhiteProcess_.alpha(s))
                     * hullWhiteProcess_.B(s, t);

            var m4 = sigma * sigma / (2 * a * a) * (dt + 2 / a * (eat - eas) - 1 / (2 * a) * (eat * eat - eas * eas));

            var m5 = -sigma * sigma / (a * a) * (dt - 1 / a * (1 - eat * ias) - 1 / (2 * a) * eaT * (iat - 2 * ias + eat * ias * ias));

            var mu = m1 + m2 + m3 + m4 + m5;

            var retVal = new Vector(3);

            var eta2 = hestonProcess_.sigma() * eta;
            var nu = hestonProcess_.kappa() * (hestonProcess_.theta() - eta * eta);

            retVal[1] = x0[1] + nu * dt + eta2 * System.Math.Sqrt(dt)
                        * (xi * dw[0] + System.Math.Sqrt(1 - xi * xi) * dw[1]);

            if (disc_ == Discretization.BSMHullWhite)
            {
                var v1 = eta * eta * dt + sigma * sigma / (a * a) * (dt - 2 / a * (1 - eat * ias)
                                                                     + 1 / (2 * a) * (1 - eat * eat * ias * ias))
                                        + 2 * sigma * eta / a * rho * (dt - 1 / a * (1 - eat * ias));
                var v2 = hullWhiteProcess_.variance(t0, r, dt);
                var v12 = (1 - eat * ias) * (sigma * eta / a * rho + sigma * sigma / (a * a))
                          - sigma * sigma / (2 * a * a) * (1 - eat * eat * ias * ias);

                Utils.QL_REQUIRE(v1 > 0.0 && v2 > 0.0, () => "zero or negative variance given");

                // terminal rho must be between -maxRho and +maxRho
                var rhoT = System.Math.Min(maxRho_, System.Math.Max(-maxRho_, v12 / System.Math.Sqrt(v1 * v2)));
                Utils.QL_REQUIRE(rhoT <= 1.0 && rhoT >= -1.0
                                 && 1 - rhoT * rhoT / (1 - xi * xi) >= 0.0, () => "invalid terminal correlation");

                var dw_0 = dw[0];
                var dw_2 = rhoT * dw[0] - rhoT * xi / System.Math.Sqrt(1 - xi * xi) * dw[1]
                           + System.Math.Sqrt(1 - rhoT * rhoT / (1 - xi * xi)) * dw[2];

                retVal[2] = hullWhiteProcess_.evolve(t0, r, dt, dw_2);

                var vol = System.Math.Sqrt(v1) * dw_0;
                retVal[0] = x0[0] * System.Math.Exp(mu + vol);
            }
            else if (disc_ == Discretization.Euler)
            {
                var dw_2 = rho * dw[0] - rho * xi / System.Math.Sqrt(1 - xi * xi) * dw[1]
                           + System.Math.Sqrt(1 - rho * rho / (1 - xi * xi)) * dw[2];

                retVal[2] = hullWhiteProcess_.evolve(t0, r, dt, dw_2);

                var vol = eta * System.Math.Sqrt(dt) * dw[0];
                retVal[0] = x0[0] * System.Math.Exp(mu + vol);
            }
            else
                Utils.QL_FAIL("unknown discretization scheme");

            return retVal;
        }

        public double numeraire(double t, Vector x) => hullWhiteModel_.discountBond(t, T_, x[2]) / endDiscount_;

        public HestonProcess hestonProcess() => hestonProcess_;

        public HullWhiteForwardProcess hullWhiteProcess() => hullWhiteProcess_;

        public double eta() => corrEquityShortRate_;

        public override double time(Date date) => hestonProcess_.time(date);

        public Discretization discretization() => disc_;

        public override void update() { endDiscount_ = hestonProcess_.riskFreeRate().link.discount(T_); }

        protected HestonProcess hestonProcess_;
        protected HullWhiteForwardProcess hullWhiteProcess_;

        //model is used to calculate P(t,T)
        protected HullWhite hullWhiteModel_;

        protected double corrEquityShortRate_;
        protected Discretization disc_;
        protected double maxRho_;
        protected double T_;
        protected double endDiscount_;

    }
}
