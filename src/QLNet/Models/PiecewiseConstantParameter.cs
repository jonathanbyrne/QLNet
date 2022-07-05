using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Models
{
    [PublicAPI]
    public class PiecewiseConstantParameter : Parameter
    {
        private new class Impl : Parameter.Impl
        {
            private readonly List<double> times_;

            public Impl(List<double> times)
            {
                times_ = times;
            }

            public override double value(Vector parameters, double t)
            {
                var size = times_.Count;
                for (var i = 0; i < size; i++)
                {
                    if (t < times_[i])
                    {
                        return parameters[i];
                    }
                }

                return parameters[size];
            }
        }

        public PiecewiseConstantParameter(List<double> times, Constraint constraint = null)
            : base(times.Count + 1, new Impl(times), constraint ?? new NoConstraint())
        {
        }
    }
}
