using JetBrains.Annotations;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class FordeHestonExpansion : HestonExpansion
    {
        private double[] coeffs = new double[5];

        public FordeHestonExpansion(double kappa, double theta, double sigma, double v0, double rho, double term)
        {
            var v0Sqrt = System.Math.Sqrt(v0);
            var rhoBarSquare = 1 - rho * rho;
            var sigma00 = v0Sqrt;
            var sigma01 = v0Sqrt * (rho * sigma / (4 * v0)); //term in x
            var sigma02 = v0Sqrt * ((1 - 5 * rho * rho / 2) / 24 * sigma * sigma / (v0 * v0)); //term in x*x
            var a00 = -sigma * sigma / 12 * (1 - rho * rho / 4) + v0 * rho * sigma / 4 + kappa / 2 * (theta - v0);
            var a01 = rho * sigma / (24 * v0) * (sigma * sigma * rhoBarSquare - 2 * kappa * (theta + v0) + v0 * rho * sigma); //term in x
            var a02 = (176 * sigma * sigma - 480 * kappa * theta - 712 * rho * rho * sigma * sigma + 521 * rho * rho * rho * rho * sigma * sigma + 40 * sigma * rho * rho * rho * v0 + 1040 * kappa * theta * rho * rho - 80 * v0 * kappa * rho * rho) * sigma * sigma / (v0 * v0 * 7680);
            coeffs[0] = sigma00 * sigma00 + a00 * term;
            coeffs[1] = sigma00 * sigma01 * 2 + a01 * term;
            coeffs[2] = sigma00 * sigma02 * 2 + sigma01 * sigma01 + a02 * term;
            coeffs[3] = sigma01 * sigma02 * 2;
            coeffs[4] = sigma02 * sigma02;
        }

        public override double impliedVolatility(double strike, double forward)
        {
            var x = System.Math.Log(strike / forward);
            var var = coeffs[0] + x * (coeffs[1] + x * (coeffs[2] + x * (coeffs[3] + x * coeffs[4])));
            var = System.Math.Max(1e-8, var);
            return System.Math.Sqrt(var);
        }
    }
}
