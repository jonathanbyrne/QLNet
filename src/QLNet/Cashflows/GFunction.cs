namespace QLNet.Cashflows
{
    public abstract class GFunction
    {
        public abstract double firstDerivative(double x);

        public abstract double secondDerivative(double x);

        public abstract double value(double x);
    }
}
