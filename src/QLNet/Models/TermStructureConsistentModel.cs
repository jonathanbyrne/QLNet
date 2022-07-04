using QLNet.Patterns;
using QLNet.Termstructures;

namespace QLNet.Models
{
    [JetBrains.Annotations.PublicAPI] public class TermStructureConsistentModel : IObservable
    {
        public TermStructureConsistentModel(Handle<YieldTermStructure> termStructure)
        {
            termStructure_ = termStructure;
        }

        public Handle<YieldTermStructure> termStructure() => termStructure_;

        private Handle<YieldTermStructure> termStructure_;

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
    }
}