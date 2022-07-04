namespace QLNet.Math
{
    [JetBrains.Annotations.PublicAPI] public class RichardsonEqn : ISolver1d
    {
        public RichardsonEqn(double fh, double ft, double fs, double t, double s)
        {
            fdelta_h_ = fh;
            ft_ = ft;
            fs_ = fs;
            t_ = t;
            s_ = s;
        }

        public override double value(double k) =>
            ft_ + (ft_ - fdelta_h_) / (System.Math.Pow(t_, k) - 1.0)
            - (fs_ + (fs_ - fdelta_h_) / (System.Math.Pow(s_, k) - 1.0));

        private double fdelta_h_, ft_, fs_, t_, s_;
    }
}