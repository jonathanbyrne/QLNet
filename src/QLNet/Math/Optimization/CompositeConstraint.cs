using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class CompositeConstraint : Constraint
    {
        private class Impl : IConstraint
        {
            private readonly Constraint c1_;
            private readonly Constraint c2_;

            public Impl(Constraint c1, Constraint c2)
            {
                c1_ = c1;
                c2_ = c2;
            }

            public Vector lowerBound(Vector parameters)
            {
                var c1lb = c1_.lowerBound(parameters);
                var c2lb = c2_.lowerBound(parameters);
                var rtrnArray = new Vector(c1lb.size(), 0.0);

                for (var iter = 0; iter < c1lb.size(); iter++)
                {
                    rtrnArray[iter] = System.Math.Max(c1lb[iter], c2lb[iter]);
                }

                return rtrnArray;
            }

            public bool test(Vector p) => c1_.test(p) && c2_.test(p);

            public Vector upperBound(Vector parameters)
            {
                var c1ub = c1_.upperBound(parameters);
                var c2ub = c2_.upperBound(parameters);
                var rtrnArray = new Vector(c1ub.size(), 0.0);

                for (var iter = 0; iter < c1ub.size(); iter++)
                {
                    rtrnArray[iter] = System.Math.Min(c1ub[iter], c2ub[iter]);
                }

                return rtrnArray;
            }
        }

        public CompositeConstraint(Constraint c1, Constraint c2) : base(new Impl(c1, c2))
        {
        }
    }
}
