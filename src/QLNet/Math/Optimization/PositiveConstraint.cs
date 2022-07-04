namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public class PositiveConstraint : Constraint
    {
        public PositiveConstraint()
            : base(new Impl())
        {
        }

        private class Impl : IConstraint
        {
            public bool test(Vector v)
            {
                for (var i = 0; i < v.Count; ++i)
                {
                    if (v[i] <= 0.0)
                        return false;
                }
                return true;
            }

            public Vector upperBound(Vector parameters) => new Vector(parameters.size(), double.MaxValue);

            public Vector lowerBound(Vector parameters) => new Vector(parameters.size(), 0.0);
        }
    }
}