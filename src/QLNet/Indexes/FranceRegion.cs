namespace QLNet.Indexes
{
    [JetBrains.Annotations.PublicAPI] public class FranceRegion : Region
    {
        public FranceRegion()
        {
            var FRdata = new Data("France", "FR");
            data_ = FRdata;
        }
    }
}