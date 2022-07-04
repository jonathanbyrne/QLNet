using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class EuropeanExercise : Exercise
    {
        public EuropeanExercise(Date date) : base(Type.European)
        {
            dates_ = new InitializedList<Date>(1, date);
        }
    }
}