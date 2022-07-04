using System.Collections.Generic;
using QLNet.Patterns;
using QLNet.Quotes;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class RendistatoBasket : IObserver, IObservable
    {

        public RendistatoBasket(List<BTP> btps, List<double> outstandings, List<Handle<Quote>> cleanPriceQuotes)
        {
            btps_ = btps;
            outstandings_ = outstandings;
            quotes_ = cleanPriceQuotes;

            Utils.QL_REQUIRE(!btps_.empty(), () => "empty RendistatoCalculator Basket");
            var k = btps_.Count;

            Utils.QL_REQUIRE(outstandings_.Count == k, () =>
                "mismatch between number of BTPs (" + k +
                ") and number of outstandings (" +
                outstandings_.Count + ")");
            Utils.QL_REQUIRE(quotes_.Count == k, () =>
                "mismatch between number of BTPs (" + k +
                ") and number of clean prices quotes (" +
                quotes_.Count + ")");

            // require non-negative outstanding
            for (var i = 0; i < k; ++i)
            {
                Utils.QL_REQUIRE(outstandings[i] >= 0, () =>
                    "negative outstanding for " + i +
                    " bond, maturity " + btps[i].maturityDate());
                // add check for prices ??
            }

            // TODO: filter out expired bonds, zero outstanding bond, etc

            Utils.QL_REQUIRE(!btps_.empty(), () => "invalid bonds only in RendistatoCalculator Basket");
            n_ = btps_.Count;

            outstanding_ = 0.0;
            for (var i = 0; i < n_; ++i)
                outstanding_ += outstandings[i];

            weights_ = new List<double>(n_);
            for (var i = 0; i < n_; ++i)
            {
                weights_.Add(outstandings[i] / outstanding_);
                quotes_[i].registerWith(update);
            }

        }
        #region Inspectors

        public int size() => n_;

        public List<BTP> btps() => btps_;

        public List<Handle<Quote>> cleanPriceQuotes() => quotes_;

        public List<double> outstandings() => outstandings_;

        public List<double> weights() => weights_;

        public double outstanding() => outstanding_;

        #endregion

        #region Observer & observable
        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        // observer interface
        public void update() { notifyObservers(); }
        #endregion


        private List<BTP> btps_;
        private List<double> outstandings_;
        private List<Handle<Quote>> quotes_;
        private double outstanding_;
        private int n_;
        private List<double> weights_;
    }
}