using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class CLPCurrency : Currency
    {
        public CLPCurrency() : base("Chilean peso", "CLP", 152, "Ch$", "", 100, new Rounding(), "%3% %1$.0f")
        {
        }
    }
}
