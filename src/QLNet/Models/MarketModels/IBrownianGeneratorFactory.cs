namespace QLNet.Models.MarketModels
{
    [JetBrains.Annotations.PublicAPI] public interface IBrownianGeneratorFactory
    {
        IBrownianGenerator create(int factors, int steps);
    }
}