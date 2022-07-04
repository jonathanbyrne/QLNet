using QLNet.Math;

namespace QLNet.Currencies
{
    /// Romanian new leu
    /// The ISO three-letter code is RON; the numeric code is 946.
    /// It is divided in 100 bani.
    [JetBrains.Annotations.PublicAPI] public class RONCurrency : Currency
    {
        public RONCurrency() : base("Romanian new leu", "RON", 946, "L", "", 100, new Rounding(), "%1$.2f %3%") { }
    }
}