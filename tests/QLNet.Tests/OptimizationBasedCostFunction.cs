using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Tests;

class OptimizationBasedCostFunction : CostFunction
{
    public override double value(Vector x) => 1.0;

    public override Vector values(Vector x)
    {
        // dummy nested optimization
        var coefficients = new Vector(3, 1.0);
        var oneDimensionalPolynomialDegreeN = new OneDimensionalPolynomialDegreeN(coefficients);
        var constraint = new NoConstraint();
        var initialValues = new Vector(1, 100.0);
        var problem = new Problem(oneDimensionalPolynomialDegreeN, constraint, initialValues);
        var optimizationMethod = new LevenbergMarquardt();
        //Simplex optimizationMethod(0.1);
        //ConjugateGradient optimizationMethod;
        //SteepestDescent optimizationMethod;
        var endCriteria = new EndCriteria(1000, 100, 1e-5, 1e-5, 1e-5);
        optimizationMethod.minimize(problem, endCriteria);
        // return dummy result
        var dummy = new Vector(1, 0);
        return dummy;
    }
}