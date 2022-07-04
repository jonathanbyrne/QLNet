using System;
using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Tests;

class OneDimensionalPolynomialDegreeN : CostFunction
{
    private Vector coefficients_;
    private int polynomialDegree_;

    public OneDimensionalPolynomialDegreeN(Vector coefficients)
    {
        coefficients_ = new Vector(coefficients);
        polynomialDegree_ = coefficients.size() - 1;
    }

    public override double value(Vector x)
    {
        if (x.size() != 1)
            throw new Exception("independent variable must be 1 dimensional");
        double y = 0;
        for (var i = 0; i <= polynomialDegree_; ++i)
            y += coefficients_[i] * Utils.Pow(x[0], i);
        return y;
    }

    public override Vector values(Vector x)
    {
        if (x.size() != 1)
            throw new Exception("independent variable must be 1 dimensional");
        var y = new Vector(1);
        y[0] = value(x);
        return y;
    }
}