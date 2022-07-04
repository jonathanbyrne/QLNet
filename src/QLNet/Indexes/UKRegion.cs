namespace QLNet.Indexes
{
    [JetBrains.Annotations.PublicAPI] public class UKRegion : Region
    {
        public UKRegion()
        {
            var UKdata = new Data("UK", "UK");
            data_ = UKdata;
        }
    }
}