using JetBrains.Annotations;

namespace QLNet.Math.Optimization
{
    [PublicAPI]
    public class LeastSquareFunction : CostFunction
    {
        //! least square problem
        protected LeastSquareProblem lsp_;

        //! Default constructor
        public LeastSquareFunction(LeastSquareProblem lsp)
        {
            lsp_ = lsp;
        }

        //! compute vector of derivatives of the least square function
        public override void gradient(ref Vector grad_f, Vector x)
        {
            // size of target and function to fit vectors
            var target = new Vector(lsp_.size());
            var fct2fit = new Vector(lsp_.size());
            // size of gradient matrix
            var grad_fct2fit = new Matrix(lsp_.size(), x.size());
            // compute its values
            lsp_.targetValueAndGradient(x, ref grad_fct2fit, ref target, ref fct2fit);
            // do the difference
            var diff = target - fct2fit;
            // compute derivative
            grad_f = -2.0 * (Matrix.transpose(grad_fct2fit) * diff);
        }

        //! compute value of the least square function
        public override double value(Vector x)
        {
            // size of target and function to fit vectors
            var target = new Vector(lsp_.size());
            var fct2fit = new Vector(lsp_.size());
            // compute its values
            lsp_.targetAndValue(x, ref target, ref fct2fit);
            // do the difference
            var diff = target - fct2fit;
            // and compute the scalar product (square of the norm)
            return Vector.DotProduct(diff, diff);
        }

        //! compute value and gradient of the least square function
        public override double valueAndGradient(ref Vector grad_f, Vector x)
        {
            // size of target and function to fit vectors
            var target = new Vector(lsp_.size());
            var fct2fit = new Vector(lsp_.size());
            // size of gradient matrix
            var grad_fct2fit = new Matrix(lsp_.size(), x.size());
            // compute its values
            lsp_.targetValueAndGradient(x, ref grad_fct2fit, ref target, ref fct2fit);
            // do the difference
            var diff = target - fct2fit;
            // compute derivative
            grad_f = -2.0 * (Matrix.transpose(grad_fct2fit) * diff);
            // and compute the scalar product (square of the norm)
            return Vector.DotProduct(diff, diff);
        }

        public override Vector values(Vector x)
        {
            // size of target and function to fit vectors
            var target = new Vector(lsp_.size());
            var fct2fit = new Vector(lsp_.size());
            // compute its values
            lsp_.targetAndValue(x, ref target, ref fct2fit);
            // do the difference
            var diff = target - fct2fit;
            return Vector.DirectMultiply(diff, diff);
        }
    }
}
