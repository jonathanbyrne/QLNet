using System.Collections.Generic;
using QLNet.Math;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Tests;

public struct VolatilityCube
{
    public SwaptionTenors tenors;
    public Matrix volSpreads;
    public List<List<Handle<Quote>>> volSpreadsHandle;
    public List<double> strikeSpreads;
    public void setMarketData()
    {
        tenors.options = new InitializedList<Period>(3);
        tenors.options[0] = new Period(1, TimeUnit.Years);
        tenors.options[1] = new Period(10, TimeUnit.Years);
        tenors.options[2] = new Period(30, TimeUnit.Years);
        tenors.swaps = new InitializedList<Period>(3);
        tenors.swaps[0] = new Period(2, TimeUnit.Years);
        tenors.swaps[1] = new Period(10, TimeUnit.Years);
        tenors.swaps[2] = new Period(30, TimeUnit.Years);
        strikeSpreads = new InitializedList<double>(5);
        strikeSpreads[0] = -0.020;
        strikeSpreads[1] = -0.005;
        strikeSpreads[2] = +0.000;
        strikeSpreads[3] = +0.005;
        strikeSpreads[4] = +0.020;
        volSpreads = new Matrix(tenors.options.Count * tenors.swaps.Count, strikeSpreads.Count);
        volSpreads[0, 0] = 0.0599; volSpreads[0, 1] = 0.0049;
        volSpreads[0, 2] = 0.0000;
        volSpreads[0, 3] = -0.0001; volSpreads[0, 4] = 0.0127;
        volSpreads[1, 0] = 0.0729; volSpreads[1, 1] = 0.0086;
        volSpreads[1, 2] = 0.0000;
        volSpreads[1, 3] = -0.0024; volSpreads[1, 4] = 0.0098;
        volSpreads[2, 0] = 0.0738; volSpreads[2, 1] = 0.0102;
        volSpreads[2, 2] = 0.0000;
        volSpreads[2, 3] = -0.0039; volSpreads[2, 4] = 0.0065;
        volSpreads[3, 0] = 0.0465; volSpreads[3, 1] = 0.0063;
        volSpreads[3, 2] = 0.0000;
        volSpreads[3, 3] = -0.0032; volSpreads[3, 4] = -0.0010;
        volSpreads[4, 0] = 0.0558; volSpreads[4, 1] = 0.0084;
        volSpreads[4, 2] = 0.0000;
        volSpreads[4, 3] = -0.0050; volSpreads[4, 4] = -0.0057;
        volSpreads[5, 0] = 0.0576; volSpreads[5, 1] = 0.0083;
        volSpreads[5, 2] = 0.0000;
        volSpreads[5, 3] = -0.0043; volSpreads[5, 4] = -0.0014;
        volSpreads[6, 0] = 0.0437; volSpreads[6, 1] = 0.0059;
        volSpreads[6, 2] = 0.0000;
        volSpreads[6, 3] = -0.0030; volSpreads[6, 4] = -0.0006;
        volSpreads[7, 0] = 0.0533; volSpreads[7, 1] = 0.0078;
        volSpreads[7, 2] = 0.0000;
        volSpreads[7, 3] = -0.0045; volSpreads[7, 4] = -0.0046;
        volSpreads[8, 0] = 0.0545; volSpreads[8, 1] = 0.0079;
        volSpreads[8, 2] = 0.0000;
        volSpreads[8, 3] = -0.0042; volSpreads[8, 4] = -0.0020;
        volSpreadsHandle = new InitializedList<List<Handle<Quote>>>(tenors.options.Count * tenors.swaps.Count);
        for (var i = 0; i < tenors.options.Count * tenors.swaps.Count; i++)
        {
            volSpreadsHandle[i] = new InitializedList<Handle<Quote>>(strikeSpreads.Count);
            for (var j = 0; j < strikeSpreads.Count; j++)
            {
                // every handle must be reassigned, as the ones created by
                // default are all linked together.
                volSpreadsHandle[i][j] = new Handle<Quote>(new SimpleQuote(volSpreads[i, j]));
            }
        }
    }
}