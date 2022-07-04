namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public class BoundaryConstraint : Constraint
    {
        public BoundaryConstraint(double low, double high)
            : base(new Impl(low, high))
        {
        }

        private class Impl : IConstraint
        {
            private double low_;
            private double high_;

            public Impl(double low, double high)
            {
                low_ = low;
                high_ = high;
            }
            public bool test(Vector v)
            {
                for (var i = 0; i < v.Count; i++)
                {
                    if (v[i] < low_ || v[i] > high_)
                        return false;
                }
                return true;
            }

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), high_);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), low_);
        }
    }
}