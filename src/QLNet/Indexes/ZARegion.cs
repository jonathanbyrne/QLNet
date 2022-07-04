namespace QLNet.Indexes
{
    [JetBrains.Annotations.PublicAPI] public class ZARegion : Region
    {
        public ZARegion()
        {
            var ZAdata = new Data("South Africa", "ZA");
            data_ = ZAdata;
        }
    }
}