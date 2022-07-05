using JetBrains.Annotations;

namespace QLNet.Models.MarketModels
{
    [PublicAPI]
    public interface IBrownianGeneratorFactory
    {
        IBrownianGenerator create(int factors, int steps);
    }
}
