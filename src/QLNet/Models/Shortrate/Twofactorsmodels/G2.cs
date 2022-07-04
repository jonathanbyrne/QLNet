/*
 Copyright (C) 2010 Philippe double (ph_real@hotmail.com)

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
using QLNet.Instruments;
using QLNet.Math;
using QLNet.Math.Distributions;
using QLNet.Math.integrals;
using QLNet.Math.Solvers1d;
using QLNet.Models.Shortrate;
using QLNet.Termstructures;
using QLNet.Time;
using System;
using System.Collections.Generic;
using QLNet.Math.Optimization;
using QLNet.processes;

// Two-factor additive Gaussian Model G2 + +

namespace QLNet.Models.Shortrate.Twofactorsmodels
{

    //! Two-additive-factor gaussian model class.
    /*! This class implements a two-additive-factor model defined by
        \f[
            dr_t = \varphi(t) + x_t + y_t
        \f]
        where \f$ x_t \f$ and \f$ y_t \f$ are defined by
        \f[
            dx_t = -a x_t dt + \sigma dW^1_t, x_0 = 0
        \f]
        \f[
            dy_t = -b y_t dt + \sigma dW^2_t, y_0 = 0
        \f]
        and \f$ dW^1_t dW^2_t = \rho dt \f$.

        \bug This class was not tested enough to guarantee
             its functionality.

        \ingroup shortrate
    */
    [JetBrains.Annotations.PublicAPI] public class G2 : TwoFactorModel,
      IAffineModel,
      ITermStructureConsistentModel
    {


        #region ITermStructureConsistentModel
        public Handle<YieldTermStructure> termStructure() => termStructure_;

        public Handle<YieldTermStructure> termStructure_ { get; set; }
        #endregion

        Parameter a_;
        Parameter sigma_;
        Parameter b_;
        Parameter eta_;
        Parameter rho_;
        Parameter phi_;

        public G2(Handle<YieldTermStructure> termStructure,
                  double a,
                  double sigma,
                  double b,
                  double eta,
                  double rho)
           : base(5)
        {
            termStructure_ = termStructure;
            a_ = arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
            sigma_ = arguments_[1] = new ConstantParameter(sigma, new PositiveConstraint());
            b_ = arguments_[2] = new ConstantParameter(b, new PositiveConstraint());
            eta_ = arguments_[3] = new ConstantParameter(eta, new PositiveConstraint());
            rho_ = arguments_[4] = new ConstantParameter(rho, new BoundaryConstraint(-1.0, 1.0));

            generateArguments();
            termStructure.registerWith(update);
        }

        public G2(Handle<YieldTermStructure> termStructure,
                  double a,
                  double sigma,
                  double b,
                  double eta)
           : this(termStructure, a, sigma, b, eta, -0.75)
        { }

        public G2(Handle<YieldTermStructure> termStructure,
                  double a,
                  double sigma,
                  double b)
           : this(termStructure, a, sigma, b, 0.01, -0.75)
        { }


        public G2(Handle<YieldTermStructure> termStructure,
                  double a,
                  double sigma)
           : this(termStructure, a, sigma, 0.1, 0.01, -0.75)
        { }

        public G2(Handle<YieldTermStructure> termStructure,
                  double a)
           : this(termStructure, a, 0.01, 0.1, 0.01, -0.75)
        { }

        public G2(Handle<YieldTermStructure> termStructure)
           : this(termStructure, 0.1, 0.01, 0.1, 0.01, -0.75)
        { }

        public override ShortRateDynamics dynamics() => new Dynamics(phi_, a(), sigma(), b(), eta(), rho());

        public virtual double discountBond(double now,
                                           double maturity,
                                           Vector factors)
        {
            Utils.QL_REQUIRE(factors.size() > 1, () => "g2 model needs two factors to compute discount bond");
            return discountBond(now, maturity, factors[0], factors[1]);
        }

        public virtual double discountBond(double t, double T, double x, double y) => A(t, T) * System.Math.Exp(-B(a(), T - t) * x - B(b(), T - t) * y);

        public virtual double discountBondOption(Option.Type type,
                                                 double strike,
                                                 double maturity,
                                                 double bondMaturity)
        {
            var v = sigmaP(maturity, bondMaturity);
            var f = termStructure().link.discount(bondMaturity);
            var k = termStructure().link.discount(maturity) * strike;

            return Utils.blackFormula(type, k, f, v);
        }

        public double discount(double t) => termStructure().currentLink().discount(t);

        public double swaption(Swaption.Arguments arguments,
                               double fixedRate,
                               double range,
                               int intervals)
        {

            var settlement = termStructure().link.referenceDate();
            var dayCounter = termStructure().link.dayCounter();
            var start = dayCounter.yearFraction(settlement,
                                                   arguments.floatingResetDates[0]);
            double w = arguments.type == VanillaSwap.Type.Payer ? 1 : -1;

            List<double> fixedPayTimes = new InitializedList<double>(arguments.fixedPayDates.Count);
            for (var i = 0; i < fixedPayTimes.Count; ++i)
                fixedPayTimes[i] =
                   dayCounter.yearFraction(settlement,
                                           arguments.fixedPayDates[i]);

            var function = new SwaptionPricingFunction(a(),
                                                                           sigma(), b(), eta(), rho(),
                                                                           w, start,
                                                                           fixedPayTimes,
                                                                           fixedRate, this);

            var upper = function.mux() + range * function.sigmax();
            var lower = function.mux() - range * function.sigmax();
            var integrator = new SegmentIntegral(intervals);
            return arguments.nominal * w * termStructure().link.discount(start) *
                   integrator.value(function.value, lower, upper);
        }

        #region protected
        protected override void generateArguments()
        {
            phi_ = new FittingParameter(termStructure(),
                                        a(), sigma(), b(), eta(), rho());
        }

        protected double A(double t, double T) =>
            termStructure().link.discount(T) / termStructure().link.discount(t) *
            System.Math.Exp(0.5 * (V(T - t) - V(T) + V(t)));

        protected double B(double x, double t) => (1.0 - System.Math.Exp(-x * t)) / x;

        #endregion

        #region private
        double sigmaP(double t, double s)
        {
            var temp = 1.0 - System.Math.Exp(-(a() + b()) * t);
            var temp1 = 1.0 - System.Math.Exp(-a() * (s - t));
            var temp2 = 1.0 - System.Math.Exp(-b() * (s - t));
            var a3 = a() * a() * a();
            var b3 = b() * b() * b();
            var sigma2 = sigma() * sigma();
            var eta2 = eta() * eta();
            var value =
               0.5 * sigma2 * temp1 * temp1 * (1.0 - System.Math.Exp(-2.0 * a() * t)) / a3 +
               0.5 * eta2 * temp2 * temp2 * (1.0 - System.Math.Exp(-2.0 * b() * t)) / b3 +
               2.0 * rho() * sigma() * eta() / (a() * b() * (a() + b())) *
               temp1 * temp2 * temp;
            return System.Math.Sqrt(value);
        }


        double V(double t)
        {
            var expat = System.Math.Exp(-a() * t);
            var expbt = System.Math.Exp(-b() * t);
            var cx = sigma() / a();
            var cy = eta() / b();
            var valuex = cx * cx * (t + (2.0 * expat - 0.5 * expat * expat - 1.5) / a());
            var valuey = cy * cy * (t + (2.0 * expbt - 0.5 * expbt * expbt - 1.5) / b());
            var value = 2.0 * rho() * cx * cy * (t + (expat - 1.0) / a()
                                                   + (expbt - 1.0) / b()
                                                 - (expat * expbt - 1.0) / (a() + b()));
            return valuex + valuey + value;
        }

        double a() => a_.value(0.0);

        double sigma() => sigma_.value(0.0);

        double b() => b_.value(0.0);

        double eta() => eta_.value(0.0);

        double rho() => rho_.value(0.0);

        #endregion

        [JetBrains.Annotations.PublicAPI] public class Dynamics : ShortRateDynamics
        {

            Parameter fitting_;
            public Dynamics(Parameter fitting,
                            double a,
                            double sigma,
                            double b,
                            double eta,
                            double rho)
               : base(new OrnsteinUhlenbeckProcess(a, sigma),
                      new OrnsteinUhlenbeckProcess(b, eta), rho)
            {
                fitting_ = fitting;
            }

            public override double shortRate(double t, double x, double y) => fitting_.value(t) + x + y;

            public override StochasticProcess process() => throw new NotImplementedException();

            //! Analytical term-structure fitting parameter \f$ \varphi(t) \f$.
            /*! \f$ \varphi(t) \f$ is analytically defined by
                \f[
                    \varphi(t) = f(t) +
                         \frac{1}{2}(\frac{\sigma(1-e^{-at})}{a})^2 +
                         \frac{1}{2}(\frac{\eta(1-e^{-bt})}{b})^2 +
                         \rho\frac{\sigma(1-e^{-at})}{a}\frac{\eta(1-e^{-bt})}{b},
                \f]
                where \f$ f(t) \f$ is the instantaneous forward rate at \f$ t \f$.
            */
        }

        [JetBrains.Annotations.PublicAPI] public class FittingParameter : TermStructureFittingParameter
        {

            private new class Impl : Parameter.Impl
            {


                public Impl(Handle<YieldTermStructure> termStructure,
                            double a,
                            double sigma,
                            double b,
                            double eta,
                            double rho)
                {
                    termStructure_ = termStructure;
                    a_ = a;
                    sigma_ = sigma;
                    b_ = b;
                    eta_ = eta;
                    rho_ = rho;
                }

                public override double value(Vector v, double t)
                {
                    var forward = termStructure_.currentLink().forwardRate(t, t,
                                                                                    Compounding.Continuous,
                                                                                    Frequency.NoFrequency);
                    var temp1 = sigma_ * (1.0 - System.Math.Exp(-a_ * t)) / a_;
                    var temp2 = eta_ * (1.0 - System.Math.Exp(-b_ * t)) / b_;
                    var value = 0.5 * temp1 * temp1 + 0.5 * temp2 * temp2 +
                                rho_ * temp1 * temp2 + forward.value();
                    return value;
                }
                Handle<YieldTermStructure> termStructure_;
                double a_, sigma_, b_, eta_, rho_;
            }

            public FittingParameter(Handle<YieldTermStructure> termStructure,
                                    double a,
                                    double sigma,
                                    double b,
                                    double eta,
                                    double rho)
               : base(
                         new Impl(termStructure, a, sigma,
                                                   b, eta, rho))
            { }
        }

        [JetBrains.Annotations.PublicAPI] public class SwaptionPricingFunction
        {

            #region private fields
            double a_, sigma_, b_, eta_, rho_, w_;
            double T_;
            List<double> t_;
            double rate_;
            int size_;
            Vector A_, Ba_, Bb_;
            double mux_, muy_, sigmax_, sigmay_, rhoxy_;
            #endregion

            public SwaptionPricingFunction(double a, double sigma,
                                           double b, double eta, double rho,
                                           double w, double start,
                                           List<double> payTimes,
                                           double fixedRate, G2 model)
            {
                a_ = a;
                sigma_ = sigma;
                b_ = b;
                eta_ = eta;
                rho_ = rho;
                w_ = w;
                T_ = start;
                t_ = payTimes;
                rate_ = fixedRate;
                size_ = t_.Count;

                A_ = new Vector(size_);
                Ba_ = new Vector(size_);
                Bb_ = new Vector(size_);


                sigmax_ = sigma_ * System.Math.Sqrt(0.5 * (1.0 - System.Math.Exp(-2.0 * a_ * T_)) / a_);
                sigmay_ = eta_ * System.Math.Sqrt(0.5 * (1.0 - System.Math.Exp(-2.0 * b_ * T_)) / b_);
                rhoxy_ = rho_ * eta_ * sigma_ * (1.0 - System.Math.Exp(-(a_ + b_) * T_)) /
                         ((a_ + b_) * sigmax_ * sigmay_);

                var temp = sigma_ * sigma_ / (a_ * a_);
                mux_ = -((temp + rho_ * sigma_ * eta_ / (a_ * b_)) * (1.0 - System.Math.Exp(-a * T_)) -
                         0.5 * temp * (1.0 - System.Math.Exp(-2.0 * a_ * T_)) -
                         rho_ * sigma_ * eta_ / (b_ * (a_ + b_)) *
                         (1.0 - System.Math.Exp(-(b_ + a_) * T_)));

                temp = eta_ * eta_ / (b_ * b_);
                muy_ = -((temp + rho_ * sigma_ * eta_ / (a_ * b_)) * (1.0 - System.Math.Exp(-b * T_)) -
                         0.5 * temp * (1.0 - System.Math.Exp(-2.0 * b_ * T_)) -
                         rho_ * sigma_ * eta_ / (a_ * (a_ + b_)) *
                         (1.0 - System.Math.Exp(-(b_ + a_) * T_)));

                for (var i = 0; i < size_; i++)
                {
                    A_[i] = model.A(T_, t_[i]);
                    Ba_[i] = model.B(a_, t_[i] - T_);
                    Bb_[i] = model.B(b_, t_[i] - T_);
                }
            }

            internal double mux() => mux_;

            internal double sigmax() => sigmax_;

            public double value(double x)
            {
                var phi = new CumulativeNormalDistribution();
                var temp = (x - mux_) / sigmax_;
                var txy = System.Math.Sqrt(1.0 - rhoxy_ * rhoxy_);

                var lambda = new Vector(size_);
                int i;
                for (i = 0; i < size_; i++)
                {
                    var tau = i == 0 ? t_[0] - T_ : t_[i] - t_[i - 1];
                    var c = i == size_ - 1 ? 1.0 + rate_ * tau : rate_ * tau;
                    lambda[i] = c * A_[i] * System.Math.Exp(-Ba_[i] * x);
                }

                var function = new SolvingFunction(lambda, Bb_);
                var s1d = new Brent();
                s1d.setMaxEvaluations(1000);
                var yb = s1d.solve(function, 1e-6, 0.00, -100.0, 100.0);

                var h1 = (yb - muy_) / (sigmay_ * txy) -
                         rhoxy_ * (x - mux_) / (sigmax_ * txy);
                var value = phi.value(-w_ * h1);


                for (i = 0; i < size_; i++)
                {
                    var h2 = h1 +
                             Bb_[i] * sigmay_ * System.Math.Sqrt(1.0 - rhoxy_ * rhoxy_);
                    var kappa = -Bb_[i] *
                                (muy_ - 0.5 * txy * txy * sigmay_ * sigmay_ * Bb_[i] +
                                 rhoxy_ * sigmay_ * (x - mux_) / sigmax_);
                    value -= lambda[i] * System.Math.Exp(kappa) * phi.value(-w_ * h2);
                }

                return System.Math.Exp(-0.5 * temp * temp) * value /
                       (sigmax_ * System.Math.Sqrt(2.0 * Const.M_PI));
            }

            [JetBrains.Annotations.PublicAPI] public class SolvingFunction : ISolver1d
            {

                Vector lambda_;
                Vector Bb_;

                public SolvingFunction(Vector lambda, Vector Bb)
                {
                    lambda_ = lambda;
                    Bb_ = Bb;
                }

                public override double value(double y)
                {
                    var value = 1.0;
                    for (var i = 0; i < lambda_.size(); i++)
                    {
                        value -= lambda_[i] * System.Math.Exp(-Bb_[i] * y);
                    }
                    return value;
                }

            }

        }

    }
}


