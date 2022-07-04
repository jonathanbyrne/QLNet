using QLNet.Methods.Finitedifferences.Operators;

namespace QLNet.Methods.Finitedifferences.Utilities
{
    [JetBrains.Annotations.PublicAPI] public class FdmZeroInnerValue : FdmInnerValueCalculator
    {
        public override double innerValue(FdmLinearOpIterator iter, double t) => 0.0;

        public override double avgInnerValue(FdmLinearOpIterator iter, double t) => 0.0;
    }
}