using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class IQDCurrency : Currency
    {
        public IQDCurrency()
            : base("Iraqi dinar", "IQD", 368, "ID", "", 1000, new Rounding(), "%2% %1$.3f") { }
    }
}