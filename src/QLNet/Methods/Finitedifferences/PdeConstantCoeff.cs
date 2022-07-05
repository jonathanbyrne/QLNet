using System;
using JetBrains.Annotations;
using QLNet.Patterns;
using QLNet.processes;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public class PdeConstantCoeff<PdeClass> : PdeSecondOrderParabolic where PdeClass : PdeSecondOrderParabolic, new()
    {
        private double diffusion_;
        private double discount_;
        private double drift_;

        public PdeConstantCoeff(GeneralizedBlackScholesProcess process, double t, double x)
        {
            var pde = (PdeClass)FastActivator<PdeClass>.Create().factory(process);
            diffusion_ = pde.diffusion(t, x);
            drift_ = pde.drift(t, x);
            discount_ = pde.discount(t, x);
        }

        public override double diffusion(double x, double y) => diffusion_;

        public override double discount(double x, double y) => discount_;

        public override double drift(double x, double y) => drift_;

        public override PdeSecondOrderParabolic factory(GeneralizedBlackScholesProcess process) => throw new NotSupportedException();
    }
}
