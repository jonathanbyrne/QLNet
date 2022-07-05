using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class NoConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), double.MinValue);

            public bool test(Vector v) => true;

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);
        }

        public NoConstraint() : base(new Impl())
        {
        }
    }
}
