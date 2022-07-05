using JetBrains.Annotations;
using QLNet.Processes;

namespace QLNet.Models.Shortrate.Onefactormodels
{
    [PublicAPI]
    public class Dynamics : OneFactorModel.ShortRateDynamics
    {
        private Parameter fitting_;

        public Dynamics(Parameter fitting, double alpha, double sigma)
            : base(new OrnsteinUhlenbeckProcess(alpha, sigma))
        {
            fitting_ = fitting;
        }

        public override double shortRate(double t, double x) => System.Math.Exp(x + fitting_.value(t));

        public override double variable(double t, double r) => System.Math.Log(r) - fitting_.value(t);
    }
}
