using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class COPCurrency : Currency
    {
        public COPCurrency() : base("Colombian peso", "COP", 170, "Col$", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}