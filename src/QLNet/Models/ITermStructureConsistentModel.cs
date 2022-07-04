using QLNet.Patterns;
using QLNet.Termstructures;

namespace QLNet.Models
{
    [JetBrains.Annotations.PublicAPI] public interface ITermStructureConsistentModel
    {
        Handle<YieldTermStructure> termStructure();
        Handle<YieldTermStructure> termStructure_ { get; set; }
        void notifyObservers();
        event Callback notifyObserversEvent;
        void registerWith(Callback handler);
        void unregisterWith(Callback handler);
        void update();
    }
}