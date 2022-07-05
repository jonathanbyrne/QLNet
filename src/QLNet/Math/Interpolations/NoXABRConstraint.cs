using JetBrains.Annotations;
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class NoXABRConstraint : XABRConstraint
    {
        private class Impl : IConstraint
        {
            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), double.MinValue);

            public bool test(Vector param) => true;

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);
        }

        public NoXABRConstraint() : base(new Impl())
        {
        }
    }
}
