namespace QLNet.Indexes
{
    [JetBrains.Annotations.PublicAPI] public class EURegion : Region
    {
        public EURegion()
        {
            var EUdata = new Data("EU", "EU");
            data_ = EUdata;
        }
    }
}