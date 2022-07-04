namespace QLNet.Indexes
{
    [JetBrains.Annotations.PublicAPI] public class USRegion : Region
    {
        public USRegion()
        {
            var USdata = new Data("USA", "US");
            data_ = USdata;
        }
    }
}