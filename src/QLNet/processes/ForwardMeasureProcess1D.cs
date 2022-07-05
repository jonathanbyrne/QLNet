using System;
using JetBrains.Annotations;

namespace QLNet.processes
{
    [PublicAPI]
    public class ForwardMeasureProcess1D : StochasticProcess1D
    {
        protected double T_;

        protected ForwardMeasureProcess1D()
        {
        }

        protected ForwardMeasureProcess1D(double T)
        {
            T_ = T;
        }

        protected ForwardMeasureProcess1D(IDiscretization1D disc)
            : base(disc)
        {
        }

        public override double diffusion(double t, double x) => throw new NotImplementedException();

        public override double drift(double t, double x) => throw new NotImplementedException();

        public double getForwardMeasureTime() => T_;

        public virtual void setForwardMeasureTime(double T)
        {
            T_ = T;
            notifyObservers();
        }

        public override double x0() => throw new NotImplementedException();
    }
}
