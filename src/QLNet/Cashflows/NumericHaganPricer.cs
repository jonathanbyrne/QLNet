using System;
using System.Collections.Generic;
using QLNet.Math.integrals;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class NumericHaganPricer : HaganPricer
    {
        private double upperLimit_;
        private double stdDeviationsForUpperLimit_;
        private double lowerLimit_;
        private double requiredStdDeviations_;
        private double precision_;
        private double refiningIntegrationTolerance_;
        private double hardUpperLimit_;

        public NumericHaganPricer(Handle<SwaptionVolatilityStructure> swaptionVol,
            GFunctionFactory.YieldCurveModel modelOfYieldCurve,
            Handle<Quote> meanReversion,
            double lowerLimit = 0.0,
            double upperLimit = 1.0,
            double precision = 1.0e-6,
            double hardUpperLimit = double.MaxValue)
            : base(swaptionVol, modelOfYieldCurve, meanReversion)
        {
            upperLimit_ = upperLimit;
            lowerLimit_ = lowerLimit;
            requiredStdDeviations_ = 8;
            precision_ = precision;
            refiningIntegrationTolerance_ = 0.0001;
            hardUpperLimit_ = hardUpperLimit;
        }

        protected override double optionletPrice(Option.Type optionType, double strike)
        {
            var integrand = new ConundrumIntegrand(vanillaOptionPricer_, rateCurve_, gFunction_, fixingDate_, paymentDate_, annuity_, swapRateValue_, strike, optionType);
            stdDeviationsForUpperLimit_ = requiredStdDeviations_;
            double a;
            double b;
            double integralValue;
            if (optionType == QLNet.Option.Type.Call)
            {
                upperLimit_ = resetUpperLimit(stdDeviationsForUpperLimit_);
                integralValue = integrate(strike, upperLimit_, integrand);
            }
            else
            {
                a = System.Math.Min(strike, lowerLimit_);
                b = strike;
                integralValue = integrate(a, b, integrand);
            }

            var dFdK = integrand.firstDerivativeOfF(strike);
            var swaptionPrice = vanillaOptionPricer_.value(strike, optionType, annuity_);

            // v. HAGAN, Conundrums..., formule 2.17a, 2.18a
            return coupon_.accrualPeriod() * (discount_ / annuity_) * ((1 + dFdK) * swaptionPrice + (int)optionType * integralValue);
        }

        public double upperLimit() => upperLimit_;

        public double stdDeviations() => stdDeviationsForUpperLimit_;

        public double integrate(double a, double b, ConundrumIntegrand integrand)
        {
            var result = .0;
            // we use the non adaptive algorithm only for semi infinite interval
            if (a > 0)
            {
                // we estimate the actual boudary by testing integrand values
                var upperBoundary = 2 * a;
                while (integrand.value(upperBoundary) > precision_)
                    upperBoundary *= 2.0;
                // sometimes b < a because of a wrong estimation of b based on stdev
                if (b > a)
                    upperBoundary = System.Math.Min(upperBoundary, b);

                var gaussKronrodNonAdaptive = new GaussKronrodNonAdaptive(precision_, 1000000, 1.0);
                // if the integration intervall is wide enough we use the
                // following change variable x -> a + (b-a)*(t/(a-b))^3
                upperBoundary = System.Math.Max(a, System.Math.Min(upperBoundary, hardUpperLimit_));
                if (upperBoundary > 2 * a)
                {
                    var variableChange = new VariableChange(integrand.value, a, upperBoundary, 3);
                    result = gaussKronrodNonAdaptive.value(variableChange.value, .0, 1.0);
                }
                else
                {
                    result = gaussKronrodNonAdaptive.value(integrand.value, a, upperBoundary);
                }

                // if the expected precision has not been reached we use the old algorithm
                if (!gaussKronrodNonAdaptive.integrationSuccess())
                {
                    var integrator = new GaussKronrodAdaptive(precision_, 100000);
                    b = System.Math.Max(a, System.Math.Min(b, hardUpperLimit_));
                    result = integrator.value(integrand.value, a, b);
                }
            } // if a < b we use the old algorithm
            else
            {
                b = System.Math.Max(a, System.Math.Min(b, hardUpperLimit_));
                var integrator = new GaussKronrodAdaptive(precision_, 100000);
                result = integrator.value(integrand.value, a, b);
            }
            return result;
        }

        public override double swapletPrice()
        {
            var today = Settings.evaluationDate();
            if (fixingDate_ <= today)
            {
                // the fixing is determined
                var Rs = coupon_.swapIndex().fixing(fixingDate_);
                var price = (gearing_ * Rs + spread_) * (coupon_.accrualPeriod() * discount_);
                return price;
            }
            else
            {
                var atmCapletPrice = optionletPrice(QLNet.Option.Type.Call, swapRateValue_);
                var atmFloorletPrice = optionletPrice(QLNet.Option.Type.Put, swapRateValue_);
                return gearing_ * (coupon_.accrualPeriod() * discount_ * swapRateValue_ + atmCapletPrice - atmFloorletPrice) + spreadLegValue_;
            }
        }

        public double resetUpperLimit(double stdDeviationsForUpperLimit)
        {
            var variance = swaptionVolatility().link.blackVariance(fixingDate_, swapTenor_, swapRateValue_);
            return swapRateValue_ * System.Math.Exp(stdDeviationsForUpperLimit * System.Math.Sqrt(variance));
        }

        public double refineIntegration(double integralValue, ConundrumIntegrand integrand)
        {
            var percDiff = 1000.0;
            while (System.Math.Abs(percDiff) < refiningIntegrationTolerance_)
            {
                stdDeviationsForUpperLimit_ += 1.0;
                var lowerLimit = upperLimit_;
                upperLimit_ = resetUpperLimit(stdDeviationsForUpperLimit_);
                var diff = integrate(lowerLimit, upperLimit_, integrand);
                percDiff = diff / integralValue;
                integralValue += diff;
            }
            return integralValue;
        }

        #region Nested classes
        [JetBrains.Annotations.PublicAPI] public class VariableChange
        {
            private double a_, width_;
            private Func<double, double> f_;
            private int k_;

            public VariableChange(Func<double, double> f, double a, double b, int k)
            {
                a_ = a;
                width_ = b - a;
                f_ = f;
                k_ = k;
            }

            public double value(double x)
            {
                double newVar;
                var temp = width_;
                for (var i = 1; i < k_; ++i)
                {
                    temp *= x;
                }
                newVar = a_ + x * temp;
                return f_(newVar) * k_ * temp;
            }
        }

        [JetBrains.Annotations.PublicAPI] public class Spy
        {
            Func<double, double> f_;
            private List<double> abscissas = new List<double>();
            private List<double> functionValues = new List<double>();

            public Spy(Func<double, double> f)
            {
                f_ = f;
            }
            public double value(double x)
            {
                abscissas.Add(x);
                var value = f_(x);
                functionValues.Add(value);
                return value;
            }
        }

        //===========================================================================//
        //                              ConundrumIntegrand                           //
        //===========================================================================//
        [JetBrains.Annotations.PublicAPI] public class ConundrumIntegrand : IValue
        {
            public ConundrumIntegrand(VanillaOptionPricer o, YieldTermStructure curve, GFunction gFunction, Date fixingDate, Date paymentDate, double annuity, double forwardValue, double strike, QLNet.Option.Type optionType)
            {
                vanillaOptionPricer_ = o;
                forwardValue_ = forwardValue;
                annuity_ = annuity;
                fixingDate_ = fixingDate;
                paymentDate_ = paymentDate;
                strike_ = strike;
                optionType_ = optionType;
                gFunction_ = gFunction;
            }

            public double value(double x)
            {
                var option = vanillaOptionPricer_.value(x, optionType_, annuity_);
                return option * secondDerivativeOfF(x);
            }

            protected double functionF(double x)
            {
                var Gx = gFunction_.value(x);
                var GR = gFunction_.value(forwardValue_);
                return (x - strike_) * (Gx / GR - 1.0);
            }

            public double firstDerivativeOfF(double x)
            {
                var Gx = gFunction_.value(x);
                var GR = gFunction_.value(forwardValue_);
                var G1 = gFunction_.firstDerivative(x);
                return Gx / GR - 1.0 + G1 / GR * (x - strike_);
            }

            public double secondDerivativeOfF(double x)
            {
                var GR = gFunction_.value(forwardValue_);
                var G1 = gFunction_.firstDerivative(x);
                var G2 = gFunction_.secondDerivative(x);
                return 2.0 * G1 / GR + (x - strike_) * G2 / GR;
            }

            protected double strike() => strike_;

            protected double annuity() => annuity_;

            protected Date fixingDate() => fixingDate_;

            protected void setStrike(double strike) { strike_ = strike; }

            protected VanillaOptionPricer vanillaOptionPricer_;
            protected double forwardValue_;
            protected double annuity_;
            protected Date fixingDate_;
            protected Date paymentDate_;
            protected double strike_;
            protected QLNet.Option.Type optionType_;
            protected GFunction gFunction_;
        }
        #endregion
    }
}