namespace QLNet.Math
{
    public abstract class ISolver1d : IValue
    {
        public abstract double value(double v);

        public virtual double derivative(double x) => 0;
    }
}
