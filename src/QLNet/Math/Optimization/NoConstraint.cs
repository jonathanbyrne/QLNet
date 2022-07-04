namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public class NoConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            public bool test(Vector v) => true;

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), double.MinValue);
        }
        public NoConstraint() : base(new Impl()) { }
    }
}