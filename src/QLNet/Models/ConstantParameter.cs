using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Models
{
    [JetBrains.Annotations.PublicAPI] public class ConstantParameter : Parameter
    {
        private new class Impl : Parameter.Impl
        {
            public override double value(Vector parameters, double UnnamedParameter1) => parameters[0];
        }
        public ConstantParameter(Constraint constraint)
            : base(1, new Impl(), constraint)
        {
        }

        public ConstantParameter(double value, Constraint constraint)
            : base(1, new Impl(), constraint)
        {
            params_[0] = value;

            Utils.QL_REQUIRE(testParams(params_), () => ": invalid value");
        }

    }
}