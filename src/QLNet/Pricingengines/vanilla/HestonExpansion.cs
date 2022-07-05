namespace QLNet.PricingEngines.vanilla
{
    public abstract class HestonExpansion
    {
        public abstract double impliedVolatility(double strike, double forward);

        ~HestonExpansion()
        {
        }
    }
}
