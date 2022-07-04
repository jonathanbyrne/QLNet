using QLNet.Math;

namespace QLNet.Currencies
{
    /// Slovak koruna
    /// The ISO three-letter code is SKK; the numeric code is 703.
    /// It is divided in 100 halierov.
    [JetBrains.Annotations.PublicAPI] public class SKKCurrency : Currency
    {
        public SKKCurrency() : base("Slovak koruna", "SKK", 703, "Sk", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}