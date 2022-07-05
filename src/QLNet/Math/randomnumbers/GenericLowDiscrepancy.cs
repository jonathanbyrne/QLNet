using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet.Math.RandomNumbers
{
    [PublicAPI]
    public class GenericLowDiscrepancy<URSG, IC> : IRSG where URSG : IRNG, new() where IC : IValue, new()
    {
        // data

        public static IC icInstance { get; set; } = FastActivator<IC>.Create();

        // more traits
        public int allowsErrorEstimate => 0;

        // factory
        public IRNG make_sequence_generator(int dimension, ulong seed)
        {
            var g = (URSG)FastActivator<URSG>.Create().factory(dimension, seed);
            return icInstance != null
                ? new InverseCumulativeRsg<URSG, IC>(g, icInstance)
                : new InverseCumulativeRsg<URSG, IC>(g);
        }
    }
}
