namespace QLNet.Pricingengines.vanilla
{
    public abstract class HestonExpansion
    {
        ~HestonExpansion() { }
        public abstract double impliedVolatility(double strike, double forward);

    }
}