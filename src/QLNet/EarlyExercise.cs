using JetBrains.Annotations;

namespace QLNet
{
    [PublicAPI]
    public class EarlyExercise : Exercise
    {
        private bool payoffAtExpiry_;

        public EarlyExercise(Type type, bool payoffAtExpiry) : base(type)
        {
            payoffAtExpiry_ = payoffAtExpiry;
        }

        public bool payoffAtExpiry() => payoffAtExpiry_;
    }
}
