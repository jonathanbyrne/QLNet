using System;
using QLNet.Quotes;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class RendistatoEquivalentSwapSpreadQuote : Quote
    {
        public RendistatoEquivalentSwapSpreadQuote(RendistatoCalculator r) { r_ = r; }
        public override double value() => r_.equivalentSwapSpread();

        public override bool isValid()
        {
            try
            {
                value();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private RendistatoCalculator r_;
    }
}