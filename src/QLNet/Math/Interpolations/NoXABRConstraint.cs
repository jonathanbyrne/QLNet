using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class NoXABRConstraint : XABRConstraint
    {
        private class Impl : IConstraint
        {
            public bool test(Vector param) => true;

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), double.MinValue);
        }

        public NoXABRConstraint() : base(new Impl())
        { }
    }
}