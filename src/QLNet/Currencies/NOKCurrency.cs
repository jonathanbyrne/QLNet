using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Currencies
{
    /// Norwegian krone
    /// The ISO three-letter code is NOK; the numeric code is 578.
    /// It is divided in 100 шre.
    [PublicAPI]
    public class NOKCurrency : Currency
    {
        public NOKCurrency() : base("Norwegian krone", "NOK", 578, "NKr", "", 100, new Rounding(), "%3% %1$.2f")
        {
        }
    }
}
