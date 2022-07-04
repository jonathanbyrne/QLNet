namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class EarlyExercise : Exercise
    {
        private bool payoffAtExpiry_;

        public bool payoffAtExpiry() => payoffAtExpiry_;

        public EarlyExercise(Type type, bool payoffAtExpiry) : base(type)
        {
            payoffAtExpiry_ = payoffAtExpiry;
        }
    }
}