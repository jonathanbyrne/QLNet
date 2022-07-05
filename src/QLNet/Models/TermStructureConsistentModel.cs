using JetBrains.Annotations;
using QLNet.Patterns;
using QLNet.Termstructures;

namespace QLNet.Models
{
    [PublicAPI]
    public class TermStructureConsistentModel : IObservable
    {
        private readonly WeakEventSource eventSource = new WeakEventSource();
        private Handle<YieldTermStructure> termStructure_;

        public TermStructureConsistentModel(Handle<YieldTermStructure> termStructure)
        {
            termStructure_ = termStructure;
        }

        public void registerWith(Callback handler)
        {
            notifyObserversEvent += handler;
        }

        public Handle<YieldTermStructure> termStructure() => termStructure_;

        public void unregisterWith(Callback handler)
        {
            notifyObserversEvent -= handler;
        }

        protected void notifyObservers()
        {
            eventSource.Raise();
        }

        public event Callback notifyObserversEvent
        {
            add => eventSource.Subscribe(value);
            remove => eventSource.Unsubscribe(value);
        }
    }
}
