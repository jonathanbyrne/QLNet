using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet.Cashflows
{
    public abstract class IborCouponPricer : FloatingRateCouponPricer
    {
        private Handle<OptionletVolatilityStructure> capletVol_;

        protected IborCouponPricer(Handle<OptionletVolatilityStructure> v = null)
        {
            capletVol_ = v ?? new Handle<OptionletVolatilityStructure>();
            if (!capletVol_.empty())
            {
                capletVol_.registerWith(update);
            }
        }

        public Handle<OptionletVolatilityStructure> capletVolatility() => capletVol_;

        public void setCapletVolatility(Handle<OptionletVolatilityStructure> v = null)
        {
            capletVol_.unregisterWith(update);
            capletVol_ = v ?? new Handle<OptionletVolatilityStructure>();
            if (!capletVol_.empty())
            {
                capletVol_.registerWith(update);
            }

            update();
        }
    }
}
