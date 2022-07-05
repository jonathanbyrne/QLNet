using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    [PublicAPI]
    public class UAHCurrency : Currency
    {
        public UAHCurrency() : base("Ukrainian hryvnia", "UAH", 980, "hrn", "", 100, new Rounding(), "%1$.2f %3%")
        {
        }
    }
}
