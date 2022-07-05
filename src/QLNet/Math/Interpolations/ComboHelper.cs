using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ComboHelper : ISectionHelper
    {
        private ISectionHelper convMonoHelper_;
        private ISectionHelper quadraticHelper_;
        private double quadraticity_;

        public ComboHelper(ISectionHelper quadraticHelper, ISectionHelper convMonoHelper, double quadraticity)
        {
            quadraticity_ = quadraticity;
            quadraticHelper_ = quadraticHelper;
            convMonoHelper_ = convMonoHelper;
            QLNet.Utils.QL_REQUIRE(quadraticity < 1.0 && quadraticity > 0.0, () => "Quadratic value must lie between 0 and 1");
        }

        public double fNext() => quadraticity_ * quadraticHelper_.fNext() + (1.0 - quadraticity_) * convMonoHelper_.fNext();

        public double primitive(double x) => quadraticity_ * quadraticHelper_.primitive(x) + (1.0 - quadraticity_) * convMonoHelper_.primitive(x);

        public double value(double x) => quadraticity_ * quadraticHelper_.value(x) + (1.0 - quadraticity_) * convMonoHelper_.value(x);
    }
}
