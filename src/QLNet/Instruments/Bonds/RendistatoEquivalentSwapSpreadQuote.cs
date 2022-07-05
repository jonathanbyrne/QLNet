using System;
using JetBrains.Annotations;
using QLNet.Quotes;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class RendistatoEquivalentSwapSpreadQuote : Quote
    {
        private RendistatoCalculator r_;

        public RendistatoEquivalentSwapSpreadQuote(RendistatoCalculator r)
        {
            r_ = r;
        }

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

        public override double value() => r_.equivalentSwapSpread();
    }
}
