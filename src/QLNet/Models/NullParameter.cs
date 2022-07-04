using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Models
{
    [JetBrains.Annotations.PublicAPI] public class NullParameter : Parameter
    {
        private new class Impl : Parameter.Impl
        {
            public override double value(Vector UnnamedParameter1, double UnnamedParameter2) => 0.0;
        }
        public NullParameter()
            : base(0, new Impl(), new NoConstraint())
        {
        }
    }
}