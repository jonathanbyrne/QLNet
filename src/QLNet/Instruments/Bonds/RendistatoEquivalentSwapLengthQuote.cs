using System;
using JetBrains.Annotations;
using QLNet.Quotes;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class RendistatoEquivalentSwapLengthQuote : Quote
    {
        private RendistatoCalculator r_;

        public RendistatoEquivalentSwapLengthQuote(RendistatoCalculator r)
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

        public override double value() => r_.equivalentSwapLength();
    }
}
