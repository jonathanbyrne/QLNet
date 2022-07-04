using QLNet.Termstructures.Volatility.swaption;

namespace QLNet
{
    public abstract class CmsCouponPricer : FloatingRateCouponPricer
    {
        protected CmsCouponPricer(Handle<SwaptionVolatilityStructure> v = null)
        {
            swaptionVol_ = v ?? new Handle<SwaptionVolatilityStructure>();
            swaptionVol_.registerWith(update);
        }

        public Handle<SwaptionVolatilityStructure> swaptionVolatility() => swaptionVol_;

        public void setSwaptionVolatility(Handle<SwaptionVolatilityStructure> v = null)
        {
            swaptionVol_.unregisterWith(update);
            swaptionVol_ = v ?? new Handle<SwaptionVolatilityStructure>();
            swaptionVol_.registerWith(update);
            update();
        }
        private Handle<SwaptionVolatilityStructure> swaptionVol_;
    }
}