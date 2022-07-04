namespace QLNet.Termstructures
{
    [JetBrains.Annotations.PublicAPI] public interface IBootStrap<T>
    {
        void setup(T ts);
        void calculate();
    }
}