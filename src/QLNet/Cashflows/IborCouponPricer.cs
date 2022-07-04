using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet
{
    public abstract class IborCouponPricer : FloatingRateCouponPricer
    {
        protected IborCouponPricer(Handle<OptionletVolatilityStructure> v = null)
        {
            capletVol_ = v ?? new Handle<OptionletVolatilityStructure>();
            if (!capletVol_.empty())
                capletVol_.registerWith(update);
        }

        public Handle<OptionletVolatilityStructure> capletVolatility() => capletVol_;

        public void setCapletVolatility(Handle<OptionletVolatilityStructure> v = null)
        {
            capletVol_.unregisterWith(update);
            capletVol_ = v ?? new Handle<OptionletVolatilityStructure>();
            if (!capletVol_.empty())
                capletVol_.registerWith(update);

            update();
        }
        private Handle<OptionletVolatilityStructure> capletVol_;
    }
}