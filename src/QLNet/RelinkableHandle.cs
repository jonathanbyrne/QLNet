using JetBrains.Annotations;
using QLNet.Patterns;

namespace QLNet
{
    [PublicAPI]
    public class RelinkableHandle<T> : Handle<T> where T : IObservable
    {
        public RelinkableHandle() : base(default(T), true)
        {
        }

        public RelinkableHandle(T h) : base(h, true)
        {
        }

        public RelinkableHandle(T h, bool registerAsObserver) : base(h, registerAsObserver)
        {
        }

        public void linkTo(T h)
        {
            linkTo(h, true);
        }

        public void linkTo(T h, bool registerAsObserver)
        {
            link_.linkTo(h, registerAsObserver);
        }
    }
}
