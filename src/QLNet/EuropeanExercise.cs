using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class EuropeanExercise : Exercise
    {
        public EuropeanExercise(Date date) : base(Type.European)
        {
            dates_ = new InitializedList<Date>(1, date);
        }
    }
}
