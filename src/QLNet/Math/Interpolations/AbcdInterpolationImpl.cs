using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.Optimization;
using QLNet.Termstructures.Volatility;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class AbcdInterpolationImpl : Interpolation.templateImpl
    {
        private AbcdCalibration abcdCalibrator_;
        private AbcdCoeffHolder abcdCoeffHolder_;
        private EndCriteria endCriteria_;
        private OptimizationMethod optMethod_;
        private bool vegaWeighted_;

        public AbcdInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,
            double a, double b, double c, double d,
            bool aIsFixed,
            bool bIsFixed,
            bool cIsFixed,
            bool dIsFixed,
            bool vegaWeighted,
            EndCriteria endCriteria,
            OptimizationMethod optMethod)
            : base(xBegin, size, yBegin)
        {
            abcdCoeffHolder_ = new AbcdCoeffHolder(a, b, c, d, aIsFixed, bIsFixed, cIsFixed, dIsFixed);
            endCriteria_ = endCriteria;
            optMethod_ = optMethod;
            vegaWeighted_ = vegaWeighted;
        }

        public AbcdCoeffHolder AbcdCoeffHolder() => abcdCoeffHolder_;

        public override double derivative(double x)
        {
            QLNet.Utils.QL_FAIL("Abcd derivative not implemented");
            return 0;
        }

        public double k(double t)
        {
            var li = new LinearInterpolation(xBegin_, size_, yBegin_);
            return li.value(t);
        }

        public override double primitive(double x)
        {
            QLNet.Utils.QL_FAIL("Abcd primitive not implemented");
            return 0;
        }

        public override double secondDerivative(double x)
        {
            QLNet.Utils.QL_FAIL("Abcd secondDerivative not implemented");
            return 0;
        }

        public override void update()
        {
            List<double> times = new List<double>(), blackVols = new List<double>();
            for (var i = 0; i < xBegin_.Count; ++i)
            {
                times.Add(xBegin_[i]);
                blackVols.Add(yBegin_[i]);
            }

            abcdCalibrator_ = new AbcdCalibration(times, blackVols,
                abcdCoeffHolder_.a_.Value,
                abcdCoeffHolder_.b_.Value,
                abcdCoeffHolder_.c_.Value,
                abcdCoeffHolder_.d_.Value,
                abcdCoeffHolder_.aIsFixed_,
                abcdCoeffHolder_.bIsFixed_,
                abcdCoeffHolder_.cIsFixed_,
                abcdCoeffHolder_.dIsFixed_,
                vegaWeighted_,
                endCriteria_,
                optMethod_);

            abcdCalibrator_.compute();
            abcdCoeffHolder_.a_ = abcdCalibrator_.a();
            abcdCoeffHolder_.b_ = abcdCalibrator_.b();
            abcdCoeffHolder_.c_ = abcdCalibrator_.c();
            abcdCoeffHolder_.d_ = abcdCalibrator_.d();
            abcdCoeffHolder_.k_ = abcdCalibrator_.k(times, blackVols);
            abcdCoeffHolder_.error_ = abcdCalibrator_.error();
            abcdCoeffHolder_.maxError_ = abcdCalibrator_.maxError();
            abcdCoeffHolder_.abcdEndCriteria_ = abcdCalibrator_.endCriteria();
        }

        public override double value(double x)
        {
            QLNet.Utils.QL_REQUIRE(x >= 0.0, () => "time must be non negative: " + x + " not allowed");
            return abcdCalibrator_.value(x);
        }
    }
}
