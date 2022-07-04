using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class FDEngineAdapter<Base, Engine> : FDVanillaEngine, IGenericEngine
        where Base : FDConditionEngineTemplate, new()
        where Engine : IGenericEngine, new()
    {

        // a wrap-up of base engine
        Base optionBase;

        // required for generics
        public FDEngineAdapter() { }

        //public FDEngineAdapter(GeneralizedBlackScholesProcess process, Size timeSteps=100, Size gridPoints=100, bool timeDependent = false)
        public FDEngineAdapter(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
        {
            optionBase = (Base)FastActivator<Base>.Create().factory(process, timeSteps, gridPoints, timeDependent);
            process.registerWith(update);
        }

        public void calculate()
        {
            optionBase.setupArguments(getArguments());
            optionBase.calculate(getResults());
        }


        #region IGenericEngine wrap-up
        // we do not need to register with the wrapped engine because all we need is containers for parameters and results
        protected IGenericEngine engine_ = FastActivator<Engine>.Create();

        public IPricingEngineArguments getArguments() => engine_.getArguments();

        public IPricingEngineResults getResults() => engine_.getResults();

        public void reset() { engine_.reset(); }
        #endregion

        #region Observer & Observable
        // observable interface
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

        public void update() { notifyObservers(); }
        #endregion
    }
}