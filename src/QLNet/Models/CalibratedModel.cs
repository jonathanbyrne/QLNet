using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Optimization;
using QLNet.Patterns;

namespace QLNet.Models
{
    [PublicAPI]
    public class CalibratedModel : IObserver, IObservable
    {
        //! Calibration cost function class
        private class CalibrationFunction : CostFunction
        {
            private readonly List<CalibrationHelper> instruments_;
            private readonly CalibratedModel model_;
            private readonly Projection projection_;
            private readonly List<double> weights_;

            public CalibrationFunction(CalibratedModel model,
                List<CalibrationHelper> instruments,
                List<double> weights,
                Projection projection)
            {
                // recheck
                model_ = model;
                instruments_ = instruments;
                weights_ = weights;
                projection_ = projection;
            }

            public override double finiteDifferenceEpsilon() => 1e-6;

            public override double value(Vector p)
            {
                model_.setParams(projection_.include(p));

                var value = 0.0;
                for (var i = 0; i < instruments_.Count; i++)
                {
                    var diff = instruments_[i].calibrationError();
                    value += diff * diff * weights_[i];
                }

                return System.Math.Sqrt(value);
            }

            public override Vector values(Vector p)
            {
                model_.setParams(projection_.include(p));

                var values = new Vector(instruments_.Count);
                for (var i = 0; i < instruments_.Count; i++)
                {
                    values[i] = instruments_[i].calibrationError() * System.Math.Sqrt(weights_[i]);
                }

                return values;
            }
        }

        //! Constraint imposed on arguments
        private class PrivateConstraint : Constraint
        {
            private class Impl : IConstraint
            {
                private readonly List<Parameter> arguments_;

                public Impl(List<Parameter> arguments)
                {
                    arguments_ = arguments;
                }

                public Vector lowerBound(Vector parameters)
                {
                    int k = 0, k2 = 0;
                    var totalSize = 0;
                    for (var i = 0; i < arguments_.Count; i++)
                    {
                        totalSize += arguments_[i].size();
                    }

                    var result = new Vector(totalSize);
                    for (var i = 0; i < arguments_.Count; i++)
                    {
                        var size = arguments_[i].size();
                        var partialParams = new Vector(size);
                        for (var j = 0; j < size; j++, k++)
                        {
                            partialParams[j] = parameters[k];
                        }

                        var tmpBound = arguments_[i].constraint().lowerBound(partialParams);
                        for (var j = 0; j < size; j++, k2++)
                        {
                            result[k2] = tmpBound[j];
                        }
                    }

                    return result;
                }

                public bool test(Vector p)
                {
                    var k = 0;
                    for (var i = 0; i < arguments_.Count; i++)
                    {
                        var size = arguments_[i].size();
                        var testParams = new Vector(size);
                        for (var j = 0; j < size; j++, k++)
                        {
                            testParams[j] = p[k];
                        }

                        if (!arguments_[i].testParams(testParams))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                public Vector upperBound(Vector parameters)
                {
                    int k = 0, k2 = 0;
                    var totalSize = 0;
                    for (var i = 0; i < arguments_.Count; i++)
                    {
                        totalSize += arguments_[i].size();
                    }

                    var result = new Vector(totalSize);
                    for (var i = 0; i < arguments_.Count; i++)
                    {
                        var size = arguments_[i].size();
                        var partialParams = new Vector(size);
                        for (var j = 0; j < size; j++, k++)
                        {
                            partialParams[j] = parameters[k];
                        }

                        var tmpBound = arguments_[i].constraint().upperBound(partialParams);
                        for (var j = 0; j < size; j++, k2++)
                        {
                            result[k2] = tmpBound[j];
                        }
                    }

                    return result;
                }
            }

            public PrivateConstraint(List<Parameter> arguments) : base(new Impl(arguments))
            {
            }
        }

        protected List<Parameter> arguments_;
        protected Constraint constraint_;
        protected EndCriteria.Type shortRateEndCriteria_;

        public CalibratedModel(int nArguments)
        {
            arguments_ = new InitializedList<Parameter>(nArguments);
            constraint_ = new PrivateConstraint(arguments_);
            shortRateEndCriteria_ = EndCriteria.Type.None;
        }

        //! Calibrate to a set of market instruments (caps/swaptions)
        /*! An additional constraint can be passed which must be
            satisfied in addition to the constraints of the model.
        */
        public void calibrate(List<CalibrationHelper> instruments,
            OptimizationMethod method,
            EndCriteria endCriteria,
            Constraint additionalConstraint = null,
            List<double> weights = null,
            List<bool> fixParameters = null)
        {
            if (weights == null)
            {
                weights = new List<double>();
            }

            if (additionalConstraint == null)
            {
                additionalConstraint = new Constraint();
            }

            QLNet.Utils.QL_REQUIRE(weights.empty() || weights.Count == instruments.Count, () =>
                "mismatch between number of instruments (" +
                instruments.Count + ") and weights(" +
                weights.Count + ")");

            Constraint c;
            if (additionalConstraint.empty())
            {
                c = constraint_;
            }
            else
            {
                c = new CompositeConstraint(constraint_, additionalConstraint);
            }

            var w = weights.Count == 0 ? new InitializedList<double>(instruments.Count, 1.0) : weights;

            var prms = parameters();
            List<bool> all = new InitializedList<bool>(prms.size(), false);
            var proj = new Projection(prms, fixParameters ?? all);
            var f = new CalibrationFunction(this, instruments, w, proj);
            var pc = new ProjectedConstraint(c, proj);
            var prob = new Problem(f, pc, proj.project(prms));
            shortRateEndCriteria_ = method.minimize(prob, endCriteria);
            var result = new Vector(prob.currentValue());
            setParams(proj.include(result));
            var shortRateProblemValues_ = prob.values(result);

            notifyObservers();
        }

        public Constraint constraint() => constraint_;

        public EndCriteria.Type endCriteria() => shortRateEndCriteria_;

        //! Returns array of arguments on which calibration is done
        public Vector parameters()
        {
            int size = 0, i;
            for (i = 0; i < arguments_.Count; i++)
            {
                size += arguments_[i].size();
            }

            var p = new Vector(size);
            var k = 0;
            for (i = 0; i < arguments_.Count; i++)
            {
                for (var j = 0; j < arguments_[i].size(); j++, k++)
                {
                    p[k] = arguments_[i].parameters()[j];
                }
            }

            return p;
        }

        public virtual void setParams(Vector parameters)
        {
            var p = 0;
            for (var i = 0; i < arguments_.Count; ++i)
            {
                for (var j = 0; j < arguments_[i].size(); ++j)
                {
                    QLNet.Utils.QL_REQUIRE(p != parameters.Count, () => "parameter array too small");
                    arguments_[i].setParam(j, parameters[p++]);
                }
            }

            QLNet.Utils.QL_REQUIRE(p == parameters.Count, () => "parameter array too big!");
            update();
        }

        public double value(Vector parameters, List<CalibrationHelper> instruments)
        {
            List<double> w = new InitializedList<double>(instruments.Count, 1.0);
            var p = new Projection(parameters);
            var f = new CalibrationFunction(this, instruments, w, p);
            return f.value(parameters);
        }

        protected virtual void generateArguments()
        {
        }

        #region Observer & Observable

        private readonly WeakEventSource eventSource = new WeakEventSource();

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        public void notifyObservers()
        {
            eventSource.Raise();
        }

        public void update()
        {
            generateArguments();
            notifyObservers();
        }

        #endregion
    }
}
