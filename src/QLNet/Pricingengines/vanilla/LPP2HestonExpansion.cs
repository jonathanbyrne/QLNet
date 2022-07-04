namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class LPP2HestonExpansion : HestonExpansion
    {
        public LPP2HestonExpansion(double kappa, double theta, double sigma, double v0, double rho, double term)
        {
            ekt = System.Math.Exp(kappa * term);
            e2kt = ekt * ekt;
            e3kt = e2kt * ekt;
            e4kt = e2kt * e2kt;
            coeffs[0] = z0(term, kappa, theta, sigma, v0, rho);
            coeffs[1] = z1(term, kappa, theta, sigma, v0, rho);
            coeffs[2] = z2(term, kappa, theta, sigma, v0, rho);
        }
        public override double impliedVolatility(double strike, double forward)
        {
            var x = System.Math.Log(strike / forward);
            var vol = coeffs[0] + x * (coeffs[1] + x * coeffs[2]);
            return System.Math.Max(1e-8, vol);
        }

        private double[] coeffs = new double[3];
        private double ekt, e2kt, e3kt, e4kt;
        private double z0(double t, double kappa, double theta, double delta, double y, double rho) =>
            (4 * System.Math.Pow(delta, 2) * kappa * (-theta - 4 * ekt * (theta + kappa * t * (theta - y)) +
                                                      e2kt * ((5 - 2 * kappa * t) * theta - 2 * y) + 2 * y) *
             ((1 + ekt * (-1 + kappa * t)) * theta + (-1 + ekt) * y) +
             128 * ekt * System.Math.Pow(kappa, 3) *
             System.Math.Pow((1 + ekt * (-1 + kappa * t)) * theta + (-1 + ekt) * y, 2) +
             32 * delta * ekt * System.Math.Pow(kappa, 2) * rho *
             ((1 + ekt * (-1 + kappa * t)) * theta + (-1 + ekt) * y) *
             ((2 + kappa * t + ekt * (-2 + kappa * t)) * theta +
              (-1 + ekt - kappa * t) * y) +
             System.Math.Pow(delta, 2) * ekt * System.Math.Pow(rho, 2) *
             (-theta + kappa * t * theta + (theta - y) / ekt + y) *
             System.Math.Pow((2 + kappa * t + ekt * (-2 + kappa * t)) * theta +
                             (-1 + ekt - kappa * t) * y, 2) +
             48 * System.Math.Pow(delta, 2) * e2kt * System.Math.Pow(kappa, 2) * System.Math.Pow(rho, 2) *
             System.Math.Pow((2 + kappa * t + ekt * (-2 + kappa * t)) * theta +
                             (-1 + ekt - kappa * t) * y, 2) /
             ((1 + ekt * (-1 + kappa * t)) * theta + (-1 + ekt) * y) -
             System.Math.Pow(delta, 2) * System.Math.Pow(rho, 2) * ((1 + ekt * (-1 + kappa * t)) * theta +
                                                                    (-1 + ekt) * y) * System.Math.Pow((2 + kappa * t + ekt * (-2 + kappa * t)) *
                 theta + (-1 + ekt - kappa * t) * y, 2) +
             2 * System.Math.Pow(delta, 2) * kappa * ((1 + ekt * (-1 + kappa * t)) * theta +
                                                      (-1 + ekt) * y) * (theta - 2 * y +
                                                                         e2kt * (-5 * theta + 2 * kappa * t * theta + 2 * y +
                                                                                 8 * System.Math.Pow(rho, 2) * ((-3 + kappa * t) * theta + y)) +
                                                                         4 * ekt * (theta + kappa * t * theta - kappa * t * y +
                                                                                    System.Math.Pow(rho, 2) * ((6 + kappa * t * (4 + kappa * t)) * theta - (2 + kappa * t * (2 + kappa * t)) * y))) -
             8 * System.Math.Pow(delta, 2) * System.Math.Pow(kappa, 2) * ((1 + ekt * (-1 + kappa * t)) * theta +
                                                                          (-1 + ekt) * y) * (theta - 2 * y +
                                                                                             e2kt * (-5 * theta + 2 * kappa * t * theta + 2 * y +
                                                                                                     8 * System.Math.Pow(rho, 2) * ((-3 + kappa * t) * theta + y)) +
                                                                                             4 * ekt * (theta + kappa * t * theta - kappa * t * y +
                                                                                                        System.Math.Pow(rho, 2) * ((6 + kappa * t * (4 + kappa * t)) * theta - (2 + kappa * t * (2 + kappa * t)) * y)))
             / (-theta + kappa * t * theta + (theta - y) / ekt + y)) /
            (128.0 * e3kt * System.Math.Pow(kappa, 5) * System.Math.Pow(t, 2) *
             System.Math.Pow((-theta + kappa * t * theta + (theta - y) / ekt + y) / (kappa * t), 1.5));

        private double z1(double t, double kappa, double theta, double delta, double y, double rho) =>
            delta * rho * (-(delta * System.Math.Pow(-1 + ekt, 2) * rho * (4 * theta - y) * y) +
                           2 * ekt * System.Math.Pow(kappa, 3) * System.Math.Pow(t, 2) * theta *
                           ((2 + 2 * ekt + delta * rho * t) * theta - (2 + delta * rho * t) * y) -
                           2 * (-1 + ekt) * kappa * (2 * theta - y) *
                           ((-1 + ekt) * (-2 + delta * rho * t) * theta +
                            (-2 + 2 * ekt + delta * rho * t) * y) +
                           System.Math.Pow(kappa, 2) * t * ((-1 + ekt) *
                               (-4 + delta * rho * t + ekt * (-12 + delta * rho * t)) * System.Math.Pow(theta, 2) +
                               2 * (-4 + 4 * e2kt + delta * rho * t + 3 * delta * ekt * rho * t) * theta *
                               y - (-4 + delta * rho * t + 2 * ekt * (2 + delta * rho * t)) * System.Math.Pow(y, 2))) /
            (8.0 * System.Math.Pow(kappa, 2) * t * System.Math.Sqrt((-theta + kappa * t * theta + (theta - y) / ekt + y) /
                                                                    (kappa * t)) * System.Math.Pow((1 + ekt * (-1 + kappa * t)) * theta + (-1 + ekt) * y,
                2));

        private double z2(double t, double kappa, double theta, double delta, double y, double rho) =>
            System.Math.Pow(delta, 2) * System.Math.Sqrt((-theta + kappa * t * theta + (theta - y) / ekt + y) / (kappa * t)) *
            (-12 * System.Math.Pow(rho, 2) * System.Math.Pow((2 + kappa * t + ekt * (-2 + kappa * t)) * theta +
                                                             (-1 + ekt - kappa * t) * y, 2) +
             (-theta + kappa * t * theta + (theta - y) / ekt + y) *
             (theta - 2 * y + e2kt *
              (-5 * theta + 2 * kappa * t * theta + 2 * y + 8 * System.Math.Pow(rho, 2) * ((-3 + kappa * t) * theta + y)) +
              4 * ekt * (theta + kappa * t * theta - kappa * t * y +
                         System.Math.Pow(rho, 2) * ((6 + kappa * t * (4 + kappa * t)) * theta - (2 + kappa * t * (2 + kappa * t)) * y))))
            / (16.0 * e2kt * System.Math.Pow(-theta + kappa * t * theta + (theta - y) / ekt + y,
                4));
    }
}