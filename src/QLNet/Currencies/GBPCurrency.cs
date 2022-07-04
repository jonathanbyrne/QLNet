using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    /// British pound sterling
    /// The ISO three-letter code is GBP; the numeric code is 826.
    /// It is divided into 100 pence.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class GBPCurrency : Currency
    {
        public GBPCurrency() : base("British pound sterling", "GBP", 826, "\xA3", "p", 100, new Rounding(), "%3% %1$.2f") { }
    }
}