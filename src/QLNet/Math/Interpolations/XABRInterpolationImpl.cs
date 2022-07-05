using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math.Optimization;
using QLNet.Math.RandomNumbers;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class XABRInterpolationImpl<Model> : Interpolation.templateImpl where Model : IModel, new()
    {
        private class XABRError : CostFunction
        {
            private readonly XABRInterpolationImpl<Model> xabr_;

            public XABRError(XABRInterpolationImpl<Model> xabr)
            {
                xabr_ = xabr;
            }

            public override double value(Vector x)
            {
                var y = xabr_.coeff_.model_.direct(x, xabr_.coeff_.paramIsFixed_, xabr_.coeff_.params_, xabr_.forward_);
                for (var i = 0; i < xabr_.coeff_.params_.Count; ++i)
                {
                    xabr_.coeff_.params_[i] = y[i];
                }

                xabr_.coeff_.updateModelInstance();
                return xabr_.interpolationSquaredError();
            }

            public override Vector values(Vector x)
            {
                var y = xabr_.coeff_.model_.direct(x, xabr_.coeff_.paramIsFixed_, xabr_.coeff_.params_, xabr_.forward_);
                for (var i = 0; i < xabr_.coeff_.params_.Count; ++i)
                {
                    xabr_.coeff_.params_[i] = y[i];
                }

                xabr_.coeff_.updateModelInstance();
                return xabr_.interpolationErrors(x);
            }
        }

        private XABRConstraint constraint_;
        private EndCriteria endCriteria_;
        private double errorAccept_;
        private double forward_;
        private int maxGuesses_;
        private OptimizationMethod optMethod_;
        private bool useMaxError_;
        private bool vegaWeighted_;

        public XABRInterpolationImpl(List<double> xBegin, int size, List<double> yBegin, double t,
            double forward, List<double?> _params,
            List<bool> paramIsFixed, bool vegaWeighted,
            EndCriteria endCriteria,
            OptimizationMethod optMethod,
            double errorAccept, bool useMaxError, int maxGuesses, List<double?> addParams = null,
            XABRConstraint constraint = null)
            : base(xBegin, size, yBegin)
        {
            endCriteria_ = endCriteria ?? new EndCriteria(60000, 100, 1e-8, 1e-8, 1e-8);
            optMethod_ = optMethod ?? new LevenbergMarquardt(1e-8, 1e-8, 1e-8);
            errorAccept_ = errorAccept;
            useMaxError_ = useMaxError;
            maxGuesses_ = maxGuesses;
            forward_ = forward;
            vegaWeighted_ = vegaWeighted;
            constraint_ = constraint ?? new NoXABRConstraint();

            coeff_ = new XABRCoeffHolder<Model>(t, forward, _params, paramIsFixed, addParams);
            coeff_.weights_ = new InitializedList<double>(size, 1.0 / size);
        }

        public XABRCoeffHolder<Model> coeff_ { get; set; }

        public override double derivative(double d)
        {
            QLNet.Utils.QL_FAIL("XABR derivative not implemented");
            return 0;
        }

        public double interpolationError()
        {
            var n = xBegin_.Count;
            var squaredError = interpolationSquaredError();
            return System.Math.Sqrt(n * squaredError / (n - 1));
        }

        // calculate weighted differences
        public Vector interpolationErrors(Vector v)
        {
            var results = new Vector(xBegin_.Count);

            for (var i = 0; i < xBegin_.Count; i++)
            {
                results[i] = (value(xBegin_[i]) - yBegin_[i]) * System.Math.Sqrt(coeff_.weights_[i]);
            }

            return results;
        }

        public double interpolationMaxError()
        {
            double error, maxError = double.MinValue;

            for (var i = 0; i < xBegin_.Count; i++)
            {
                error = System.Math.Abs(value(xBegin_[i]) - yBegin_[i]);
                maxError = System.Math.Max(maxError, error);
            }

            return maxError;
        }

        // calculate total squared weighted difference (L2 norm)
        public double interpolationSquaredError()
        {
            double error, totalError = 0.0;
            for (var i = 0; i < xBegin_.Count; i++)
            {
                error = value(xBegin_[i]) - yBegin_[i];
                totalError += error * error * coeff_.weights_[i];
            }

            return totalError;
        }

        public override double primitive(double d)
        {
            QLNet.Utils.QL_FAIL("XABR primitive not implemented");
            return 0;
        }

        public override double secondDerivative(double d)
        {
            QLNet.Utils.QL_FAIL("XABR secondDerivative not implemented");
            return 0;
        }

        public override void update()
        {
            coeff_.updateModelInstance();

            // we should also check that y contains positive values only

            // we must update weights if it is vegaWeighted
            if (vegaWeighted_)
            {
                coeff_.weights_.Clear();
                var weightsSum = 0.0;

                for (var i = 0; i < xBegin_.Count; i++)
                {
                    var stdDev = System.Math.Sqrt(yBegin_[i] * yBegin_[i] * coeff_.t_);
                    coeff_.weights_.Add(coeff_.model_.weight(xBegin_[i], forward_, stdDev, coeff_.addParams_));
                    weightsSum += coeff_.weights_.Last();
                }

                // weight normalization
                for (var i = 0; i < coeff_.weights_.Count; i++)
                {
                    coeff_.weights_[i] /= weightsSum;
                }
            }

            // there is nothing to optimize
            if (coeff_.paramIsFixed_.Aggregate((a, b) => b && a))
            {
                coeff_.error_ = interpolationError();
                coeff_.maxError_ = interpolationMaxError();
                coeff_.XABREndCriteria_ = EndCriteria.Type.None;
                return;
            }

            var costFunction = new XABRError(this);

            var guess = new Vector(coeff_.model_.dimension());
            for (var i = 0; i < guess.size(); ++i)
            {
                guess[i] = coeff_.params_[i].GetValueOrDefault();
            }

            var iterations = 0;
            var freeParameters = 0;
            var bestError = double.MaxValue;
            var bestParameters = new Vector();
            for (var i = 0; i < coeff_.model_.dimension(); ++i)
            {
                if (!coeff_.paramIsFixed_[i])
                {
                    ++freeParameters;
                }
            }

            var halton = new HaltonRsg(freeParameters, 42);
            EndCriteria.Type tmpEndCriteria;
            double tmpInterpolationError;

            do
            {
                if (iterations > 0)
                {
                    var s = halton.nextSequence();
                    coeff_.model_.guess(guess, coeff_.paramIsFixed_, forward_, coeff_.t_, s.value, coeff_.addParams_);
                    for (var i = 0; i < coeff_.paramIsFixed_.Count; ++i)
                    {
                        if (coeff_.paramIsFixed_[i])
                        {
                            guess[i] = coeff_.params_[i].GetValueOrDefault();
                        }
                    }
                }

                var inversedTransformatedGuess =
                    new Vector(coeff_.model_.inverse(guess, coeff_.paramIsFixed_, coeff_.params_, forward_));

                var rainedXABRError = new ProjectedCostFunction(costFunction,
                    inversedTransformatedGuess,
                    coeff_.paramIsFixed_);

                var projectedGuess = new Vector(rainedXABRError.project(inversedTransformatedGuess));

                constraint_.config(rainedXABRError, coeff_, forward_);
                var problem = new Problem(rainedXABRError, constraint_, projectedGuess);
                tmpEndCriteria = optMethod_.minimize(problem, endCriteria_);
                var projectedResult = new Vector(problem.currentValue());
                var transfResult = new Vector(rainedXABRError.include(projectedResult));
                var result = coeff_.model_.direct(transfResult, coeff_.paramIsFixed_, coeff_.params_, forward_);
                tmpInterpolationError = useMaxError_
                    ? interpolationMaxError()
                    : interpolationError();

                if (tmpInterpolationError < bestError)
                {
                    bestError = tmpInterpolationError;
                    bestParameters = result;
                    coeff_.XABREndCriteria_ = tmpEndCriteria;
                }
            } while (++iterations < maxGuesses_ &&
                     tmpInterpolationError > errorAccept_);

            for (var i = 0; i < bestParameters.size(); ++i)
            {
                coeff_.params_[i] = bestParameters[i];
            }

            coeff_.error_ = interpolationError();
            coeff_.maxError_ = interpolationMaxError();
        }

        public override double value(double x) => coeff_.modelInstance_.volatility(x);
    }
}
