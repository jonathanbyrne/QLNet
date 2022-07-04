namespace QLNet.Methods.Finitedifferences.Solvers
{
    [JetBrains.Annotations.PublicAPI] public class FdmSchemeDesc
    {
        public enum FdmSchemeType
        {
            HundsdorferType, DouglasType,
            CraigSneydType, ModifiedCraigSneydType,
            ImplicitEulerType, ExplicitEulerType,
            MethodOfLinesType, TrBDF2Type,
            CrankNicolsonType
        }

        public FdmSchemeDesc() { }
        public FdmSchemeDesc(FdmSchemeType type, double theta, double mu)
        {
            type_ = type;
            theta_ = theta;
            mu_ = mu;
        }

        public FdmSchemeType type => type_;

        public double theta => theta_;

        public double mu => mu_;

        protected FdmSchemeType type_;
        private double theta_, mu_;

        // some default scheme descriptions
        public FdmSchemeDesc Douglas() => new FdmSchemeDesc(FdmSchemeType.DouglasType, 0.5, 0.0);

        public FdmSchemeDesc CrankNicolson() => new FdmSchemeDesc(FdmSchemeType.CrankNicolsonType, 0.5, 0.0);

        public FdmSchemeDesc ImplicitEuler() => new FdmSchemeDesc(FdmSchemeType.ImplicitEulerType, 0.0, 0.0);

        public FdmSchemeDesc ExplicitEuler() => new FdmSchemeDesc(FdmSchemeType.ExplicitEulerType, 0.0, 0.0);

        public FdmSchemeDesc CraigSneyd() => new FdmSchemeDesc(FdmSchemeType.CraigSneydType, 0.5, 0.5);

        public FdmSchemeDesc ModifiedCraigSneyd() => new FdmSchemeDesc(FdmSchemeType.ModifiedCraigSneydType, 1.0 / 3.0, 1.0 / 3.0);

        public FdmSchemeDesc Hundsdorfer() => new FdmSchemeDesc(FdmSchemeType.HundsdorferType, 0.5 + System.Math.Sqrt(3.0) / 6.0, 0.5);

        public FdmSchemeDesc ModifiedHundsdorfer() => new FdmSchemeDesc(FdmSchemeType.HundsdorferType, 1.0 - System.Math.Sqrt(2.0) / 2.0, 0.5);

        public FdmSchemeDesc MethodOfLines(double eps = 0.001, double relInitStepSize = 0.01) => new FdmSchemeDesc(FdmSchemeType.MethodOfLinesType, eps, relInitStepSize);

        public FdmSchemeDesc TrBDF2() => new FdmSchemeDesc(FdmSchemeType.TrBDF2Type, 2.0 - System.Math.Sqrt(2.0), 1E-8);
    }
}