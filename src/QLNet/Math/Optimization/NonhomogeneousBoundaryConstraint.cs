namespace QLNet.Math.Optimization
{
    [JetBrains.Annotations.PublicAPI] public class NonhomogeneousBoundaryConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            public Impl(Vector low, Vector high)
            {
                low_ = low;
                high_ = high;
                Utils.QL_REQUIRE(low_.Count == high_.Count, () => "Upper and lower boundaries sizes are inconsistent.");
            }

            public bool test(Vector parameters)
            {
                Utils.QL_REQUIRE(parameters.size() == low_.Count, () =>
                    "Number of parameters and boundaries sizes are inconsistent.");

                for (var i = 0; i < parameters.size(); i++)
                {
                    if (parameters[i] < low_[i] || parameters[i] > high_[i])
                        return false;
                }
                return true;
            }

            public Vector upperBound(Vector v) => high_;

            public Vector lowerBound(Vector v) => low_;

            private Vector low_, high_;
        }

        public NonhomogeneousBoundaryConstraint(Vector low, Vector high)
            : base(new Impl(low, high))
        { }
    }
}