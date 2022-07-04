using QLNet.Math;

namespace QLNet.Currencies
{
    [JetBrains.Annotations.PublicAPI] public class PEICurrency : Currency
    {
        public PEICurrency() : base("Peruvian inti", "PEI", 998, "I/.", "", 100, new Rounding(), "%3% %1$.2f") { }
    }
}