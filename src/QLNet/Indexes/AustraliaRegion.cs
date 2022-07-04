namespace QLNet.Indexes
{
    [JetBrains.Annotations.PublicAPI] public class AustraliaRegion : Region
    {
        public AustraliaRegion()
        {
            var AUdata = new Data("Australia", "AU");
            data_ = AUdata;
        }

    }
}