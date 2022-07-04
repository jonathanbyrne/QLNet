namespace QLNet.Tests;

public struct AmericanOptionData
{
    public QLNet.Option.Type type;
    public double strike;
    public double s; // spot
    public double q; // dividend
    public double r; // risk-free rate
    public double t; // time to maturity
    public double v; // volatility
    public double result; // expected result

    public AmericanOptionData(Option.Type type_,
        double strike_,
        double s_,
        double q_,
        double r_,
        double t_,
        double v_,
        double result_)
    {
        type = type_;
        strike = strike_;
        s = s_;
        q = q_;
        r = r_;
        t = t_;
        v = v_;
        result = result_;
    }
}