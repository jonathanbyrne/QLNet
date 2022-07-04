using System;

namespace QLNet.processes
{
    [JetBrains.Annotations.PublicAPI] public class ForwardMeasureProcess1D : StochasticProcess1D
    {
        public virtual void setForwardMeasureTime(double T)
        {
            T_ = T;
            notifyObservers();
        }
        public double getForwardMeasureTime() => T_;

        protected ForwardMeasureProcess1D() { }
        protected ForwardMeasureProcess1D(double T)
        {
            T_ = T;
        }
        protected ForwardMeasureProcess1D(IDiscretization1D disc)
            : base(disc) { }

        protected double T_;
        public override double x0() => throw new NotImplementedException();

        public override double drift(double t, double x) => throw new NotImplementedException();

        public override double diffusion(double t, double x) => throw new NotImplementedException();
    }
}