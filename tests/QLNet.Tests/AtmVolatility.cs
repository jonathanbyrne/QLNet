using System.Collections.Generic;
using QLNet.Math;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Tests;

public struct AtmVolatility
{
    public SwaptionTenors tenors;
    public Matrix vols;
    public List<List<Handle<Quote>>> volsHandle;
    public void setMarketData()
    {
        tenors.options = new InitializedList<Period>(6);
        tenors.options[0] = new Period(1, TimeUnit.Months);
        tenors.options[1] = new Period(6, TimeUnit.Months);
        tenors.options[2] = new Period(1, TimeUnit.Years);
        tenors.options[3] = new Period(5, TimeUnit.Years);
        tenors.options[4] = new Period(10, TimeUnit.Years);
        tenors.options[5] = new Period(30, TimeUnit.Years);
        tenors.swaps = new InitializedList<Period>(4);
        tenors.swaps[0] = new Period(1, TimeUnit.Years);
        tenors.swaps[1] = new Period(5, TimeUnit.Years);
        tenors.swaps[2] = new Period(10, TimeUnit.Years);
        tenors.swaps[3] = new Period(30, TimeUnit.Years);
        vols = new Matrix(tenors.options.Count, tenors.swaps.Count);
        vols[0, 0] = 0.1300; vols[0, 1] = 0.1560; vols[0, 2] = 0.1390; vols[0, 3] = 0.1220;
        vols[1, 0] = 0.1440; vols[1, 1] = 0.1580; vols[1, 2] = 0.1460; vols[1, 3] = 0.1260;
        vols[2, 0] = 0.1600; vols[2, 1] = 0.1590; vols[2, 2] = 0.1470; vols[2, 3] = 0.1290;
        vols[3, 0] = 0.1640; vols[3, 1] = 0.1470; vols[3, 2] = 0.1370; vols[3, 3] = 0.1220;
        vols[4, 0] = 0.1400; vols[4, 1] = 0.1300; vols[4, 2] = 0.1250; vols[4, 3] = 0.1100;
        vols[5, 0] = 0.1130; vols[5, 1] = 0.1090; vols[5, 2] = 0.1070; vols[5, 3] = 0.0930;
        volsHandle = new InitializedList<List<Handle<Quote>>>(tenors.options.Count);
        for (var i = 0; i < tenors.options.Count; i++)
        {
            volsHandle[i] = new InitializedList<Handle<Quote>>(tenors.swaps.Count);
            for (var j = 0; j < tenors.swaps.Count; j++)
                // every handle must be reassigned, as the ones created by
                // default are all linked together.
                volsHandle[i][j] = new Handle<Quote>(new SimpleQuote(vols[i, j]));
        }
    }
}