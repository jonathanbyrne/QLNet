namespace QLNet.Instruments
{
    public struct DoubleBarrier
    {
        public enum Type
        {
            KnockIn,
            KnockOut,
            KIKO, //! lower barrier KI, upper KO
            KOKI //! lower barrier KO, upper KI
        }
    }
}
