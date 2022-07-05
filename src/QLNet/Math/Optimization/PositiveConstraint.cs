using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class PositiveConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), 0.0);

            public bool test(Vector v)
            {
                for (var i = 0; i < v.Count; ++i)
                {
                    if (v[i] <= 0.0)
                    {
                        return false;
                    }
                }

                return true;
            }

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);
        }

        public PositiveConstraint()
            : base(new Impl())
        {
        }
    }
}
