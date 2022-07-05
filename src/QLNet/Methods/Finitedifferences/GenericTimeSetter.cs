using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public class GenericTimeSetter<PdeClass> : TridiagonalOperator.TimeSetter where PdeClass : PdeSecondOrderParabolic, new()
    {
        private LogGrid grid_;
        private PdeClass pde_;

        public GenericTimeSetter(Vector grid, GeneralizedBlackScholesProcess process)
        {
            grid_ = new LogGrid(grid);
            pde_ = (PdeClass)FastActivator<PdeClass>.Create().factory(process);
        }

        public override void setTime(double t, IOperator L)
        {
            pde_.generateOperator(t, grid_, (TridiagonalOperator)L);
        }
    }
}
