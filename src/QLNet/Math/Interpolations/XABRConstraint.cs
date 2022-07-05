using JetBrains.Annotations;
using QLNet.Math.Optimization;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class XABRConstraint : Constraint
    {
        public XABRConstraint() : base(null)
        {
        }

        public XABRConstraint(IConstraint impl)
            : base(impl)
        {
        }

        public virtual void config<Model>(ProjectedCostFunction costFunction, XABRCoeffHolder<Model> coeff,
            double forward)
            where Model : IModel, new()
        {
        }
    }
}
