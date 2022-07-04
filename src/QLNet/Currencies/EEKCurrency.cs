using QLNet.Math;

namespace QLNet.Currencies
{
    /// <summary>
    /// Estonian kroon
    /// The ISO three-letter code is EEK; the numeric code is 233.
    /// It is divided in 100 senti.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class EEKCurrency : Currency
    {
        public EEKCurrency() : base("Estonian kroon", "EEK", 233, "KR", "", 100, new Rounding(), "%1$.2f %2%") { }
    }
}