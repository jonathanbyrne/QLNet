using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class BoundaryConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            private readonly double high_;
            private readonly double low_;

            public Impl(double low, double high)
            {
                low_ = low;
                high_ = high;
            }

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), low_);

            public bool test(Vector v)
            {
                for (var i = 0; i < v.Count; i++)
                {
                    if (v[i] < low_ || v[i] > high_)
                    {
                        return false;
                    }
                }

                return true;
            }

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), high_);
        }

        public BoundaryConstraint(double low, double high)
            : base(new Impl(low, high))
        {
        }
    }
}
