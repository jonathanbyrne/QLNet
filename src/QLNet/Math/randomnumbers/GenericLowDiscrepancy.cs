using QLNet.Patterns;

namespace QLNet.Math.randomnumbers
{
    [JetBrains.Annotations.PublicAPI] public class GenericLowDiscrepancy<URSG, IC> : IRSG where URSG : IRNG, new() where IC : IValue, new()
    {
        // data
        private static IC icInstance_ = FastActivator<IC>.Create();
        public static IC icInstance
        {
            get => icInstance_;
            set => icInstance_ = value;
        }


        // more traits
        public int allowsErrorEstimate => 0;

        // factory
        public IRNG make_sequence_generator(int dimension, ulong seed)
        {
            var g = (URSG)FastActivator<URSG>.Create().factory(dimension, seed);
            return icInstance != null ? new InverseCumulativeRsg<URSG, IC>(g, icInstance)
                : new InverseCumulativeRsg<URSG, IC>(g);
        }
    }
}