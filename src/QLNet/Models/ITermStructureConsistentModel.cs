using JetBrains.Annotations;
using QLNet.Patterns;
using QLNet.Termstructures;

namespace QLNet.Models
{
    [PublicAPI]
    public interface ITermStructureConsistentModel
    {
        Handle<YieldTermStructure> termStructure_ { get; set; }

        void notifyObservers();

        void registerWith(Callback handler);

        Handle<YieldTermStructure> termStructure();

        void unregisterWith(Callback handler);

        void update();

        event Callback notifyObserversEvent;
    }
}
