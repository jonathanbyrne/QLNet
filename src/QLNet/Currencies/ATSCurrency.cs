using QLNet.Math;

namespace QLNet.Currencies
{
    /// Austrian shilling
    /// The ISO three-letter code was ATS; the numeric code was 40.
    /// It was divided in 100 groschen.
    /// Obsoleted by the Euro since 1999.
    [JetBrains.Annotations.PublicAPI] public class ATSCurrency : Currency
    {
        public ATSCurrency() : base("Austrian shilling", "ATS", 40, "", "", 100, new Rounding(), "%2% %1$.2f", new EURCurrency()) { }
    }
}