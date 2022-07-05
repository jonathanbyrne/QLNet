namespace QLNet.Instruments
{
    public struct Settlement
    {
        public enum Method
        {
            PhysicalOTC,
            PhysicalCleared,
            CollateralizedCashPrice,
            ParYieldCurve
        }

        public enum Type
        {
            Physical,
            Cash
        }

        public static void checkTypeAndMethodConsistency(Type settlementType, Method settlementMethod)
        {
            if (settlementType == Type.Physical)
            {
                QLNet.Utils.QL_REQUIRE(settlementMethod == Method.PhysicalOTC ||
                                                settlementMethod == Method.PhysicalCleared,
                    () => "invalid settlement method for physical settlement");
            }

            if (settlementType == Type.Cash)
            {
                QLNet.Utils.QL_REQUIRE(settlementMethod == Method.CollateralizedCashPrice ||
                                                settlementMethod == Method.ParYieldCurve,
                    () => "invalid settlement method for cash settlement");
            }
        }
    }
}
