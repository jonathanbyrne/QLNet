using QLNet.Time;

namespace QLNet.Tests;

internal struct Datum
{
    public Date date;
    public double rate;

    public Datum(Date d, double r)
    {
        date = d;
        rate = r;
    }
}