using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class NonhomogeneousBoundaryConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            private readonly Vector high_;
            private readonly Vector low_;

            public Impl(Vector low, Vector high)
            {
                low_ = low;
                high_ = high;
                QLNet.Utils.QL_REQUIRE(low_.Count == high_.Count, () => "Upper and lower boundaries sizes are inconsistent.");
            }

            public Vector lowerBound(Vector v) => low_;

            public bool test(Vector parameters)
            {
                QLNet.Utils.QL_REQUIRE(parameters.size() == low_.Count, () =>
                    "Number of parameters and boundaries sizes are inconsistent.");

                for (var i = 0; i < parameters.size(); i++)
                {
                    if (parameters[i] < low_[i] || parameters[i] > high_[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public Vector upperBound(Vector v) => high_;
        }

        public NonhomogeneousBoundaryConstraint(Vector low, Vector high)
            : base(new Impl(low, high))
        {
        }
    }
}
