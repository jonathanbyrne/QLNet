using QLNet.Cashflows;
using QLNet.Patterns;

namespace QLNet
{
    public abstract class FloatingRateCouponPricer : IObservable, IObserver
    {
        public abstract double capletPrice(double effectiveCap);

        public abstract double capletRate(double effectiveCap);

        public abstract double floorletPrice(double effectiveFloor);

        public abstract double floorletRate(double effectiveFloor);

        public abstract void initialize(FloatingRateCoupon coupon);

        // required interface
        public abstract double swapletPrice();

        public abstract double swapletRate();

        #region Observer & observable

        private readonly WeakEventSource eventSource = new WeakEventSource();

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        // observer interface
        public void update()
        {
            notifyObservers();
        }

        #endregion
    }
}
