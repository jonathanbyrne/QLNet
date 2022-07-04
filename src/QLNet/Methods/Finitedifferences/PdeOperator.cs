using QLNet.Math;
using QLNet.processes;

namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public class PdeOperator<PdeClass> : TridiagonalOperator where PdeClass : PdeSecondOrderParabolic, new()
    {
        public PdeOperator(Vector grid, GeneralizedBlackScholesProcess process) : this(grid, process, 0) { }
        public PdeOperator(Vector grid, GeneralizedBlackScholesProcess process, double residualTime)
            : base(grid.size())
        {
            timeSetter_ = new GenericTimeSetter<PdeClass>(grid, process);
            setTime(residualTime);
        }
    }
}