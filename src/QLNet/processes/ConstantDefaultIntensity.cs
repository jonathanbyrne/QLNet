namespace QLNet.processes
{
    [JetBrains.Annotations.PublicAPI] public class ConstantDefaultIntensity : Defaultable
    {
        private double constant_;
        private double recovery_;
        public ConstantDefaultIntensity(double constant) : this(constant, 0.0)
        {
        }
        public ConstantDefaultIntensity(double constant, double recovery)
        {
            constant_ = constant;
            recovery_ = recovery;
        }
        public override double hazardRate(double t, double s) => constant_;

        public override double defaultRecovery(double t, double s) => recovery_;
    }
}