using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class BRLCurrency : Currency
    {
        public BRLCurrency() : base("Brazilian real", "BRL", 986, "R$", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}