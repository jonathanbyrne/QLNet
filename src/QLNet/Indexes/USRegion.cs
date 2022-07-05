using JetBrains.Annotations;

namespace QLNet.Indexes
{
    [PublicAPI]
    public class USRegion : Region
    {
        public USRegion()
        {
            var USdata = new Data("USA", "US");
            data_ = USdata;
        }
    }
}
