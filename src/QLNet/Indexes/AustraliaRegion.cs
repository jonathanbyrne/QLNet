using JetBrains.Annotations;

namespace QLNet.Indexes
{
    [PublicAPI]
    public class AustraliaRegion : Region
    {
        public AustraliaRegion()
        {
            var AUdata = new Data("Australia", "AU");
            data_ = AUdata;
        }
    }
}
