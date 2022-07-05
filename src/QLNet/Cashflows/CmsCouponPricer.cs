using QLNet.Termstructures.Volatility.swaption;

namespace QLNet.Cashflows
{
    public abstract class CmsCouponPricer : FloatingRateCouponPricer
    {
        private Handle<SwaptionVolatilityStructure> swaptionVol_;

        protected CmsCouponPricer(Handle<SwaptionVolatilityStructure> v = null)
        {
            swaptionVol_ = v ?? new Handle<SwaptionVolatilityStructure>();
            swaptionVol_.registerWith(update);
        }

        public void setSwaptionVolatility(Handle<SwaptionVolatilityStructure> v = null)
        {
            swaptionVol_.unregisterWith(update);
            swaptionVol_ = v ?? new Handle<SwaptionVolatilityStructure>();
            swaptionVol_.registerWith(update);
            update();
        }

        public Handle<SwaptionVolatilityStructure> swaptionVolatility() => swaptionVol_;
    }
}
