using System;
using QLNet.Quotes;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class RendistatoEquivalentSwapLengthQuote : Quote
    {
        public RendistatoEquivalentSwapLengthQuote(RendistatoCalculator r) { r_ = r; }
        public override double value() => r_.equivalentSwapLength();

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