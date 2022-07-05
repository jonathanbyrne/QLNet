using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Math;
using QLNet.Math.Interpolations;
using QLNet.Patterns;
using QLNet.Time;

namespace QLNet.Termstructures.Inflation
{
    [PublicAPI]
    public class InterpolatedCPICapFloorTermPriceSurface<Interpolator2D> : CPICapFloorTermPriceSurface
        where Interpolator2D : IInterpolationFactory2D, new()
    {
        // data for surfaces and curve
        protected List<double> allStrikes_;
        protected Interpolation2D capPrice_, floorPrice_;
        protected Matrix cPriceB_;
        protected Matrix fPriceB_;
        protected Interpolator2D interpolator2d_;

        public InterpolatedCPICapFloorTermPriceSurface(double nominal,
            double startRate,
            Period observationLag,
            Calendar cal,
            BusinessDayConvention bdc,
            DayCounter dc,
            Handle<ZeroInflationIndex> zii,
            Handle<YieldTermStructure> yts,
            List<double> cStrikes,
            List<double> fStrikes,
            List<Period> cfMaturities,
            Matrix cPrice,
            Matrix fPrice)
            : base(nominal, startRate, observationLag, cal, bdc, dc, zii, yts, cStrikes, fStrikes, cfMaturities, cPrice, fPrice)
        {
            interpolator2d_ = FastActivator<Interpolator2D>.Create();

            performCalculations();
        }

        public override double capPrice(Date d, double k)
        {
            var t = timeFromReference(d);
            return capPrice_.value(t, k);
        }

        public override double floorPrice(Date d, double k)
        {
            var t = timeFromReference(d);
            return floorPrice_.value(t, k);
        }

        //! remember that the strikes use the quoting convention
        public override double price(Date d, double k)
        {
            var atm = zeroInflationIndex().link.zeroInflationTermStructure().link.zeroRate(d);
            return k > atm ? capPrice(d, k) : floorPrice(d, k);
        }

        // LazyObject interface
        public override void update()
        {
            notifyObservers();
        }

        //! set up the interpolations for capPrice_ and floorPrice_
        //! since we know ATM, and we have single flows,
        //! we can use put/call parity to extend the surfaces
        //! across all strikes
        protected override void performCalculations()
        {
            allStrikes_ = new List<double>();
            int nMat = cfMaturities_.Count,
                ncK = cStrikes_.Count,
                nfK = fStrikes_.Count,
                nK = ncK + nfK;
            Matrix cP = new Matrix(nK, nMat),
                fP = new Matrix(nK, nMat);
            var zts = zii_.link.zeroInflationTermStructure();
            var yts = nominalTermStructure();
            QLNet.Utils.QL_REQUIRE(!zts.empty(), () => "Zts is empty!!!");
            QLNet.Utils.QL_REQUIRE(!yts.empty(), () => "Yts is empty!!!");

            for (var i = 0; i < nfK; i++)
            {
                allStrikes_.Add(fStrikes_[i]);
                for (var j = 0; j < nMat; j++)
                {
                    var mat = cfMaturities_[j];
                    var df = yts.link.discount(cpiOptionDateFromTenor(mat));
                    var atm_quote = zts.link.zeroRate(cpiOptionDateFromTenor(mat));
                    var atm = System.Math.Pow(1.0 + atm_quote, mat.length());
                    var S = atm * df;
                    var K_quote = fStrikes_[i] / 100.0;
                    var K = System.Math.Pow(1.0 + K_quote, mat.length());
                    cP[i, j] = fPrice_[i, j] + S - K * df;
                    fP[i, j] = fPrice_[i, j];
                }
            }

            for (var i = 0; i < ncK; i++)
            {
                allStrikes_.Add(cStrikes_[i]);
                for (var j = 0; j < nMat; j++)
                {
                    var mat = cfMaturities_[j];
                    var df = yts.link.discount(cpiOptionDateFromTenor(mat));
                    var atm_quote = zts.link.zeroRate(cpiOptionDateFromTenor(mat));
                    var atm = System.Math.Pow(1.0 + atm_quote, mat.length());
                    var S = atm * df;
                    var K_quote = cStrikes_[i] / 100.0;
                    var K = System.Math.Pow(1.0 + K_quote, mat.length());
                    cP[i + nfK, j] = cPrice_[i, j];
                    fP[i + nfK, j] = cPrice_[i, j] + K * df - S;
                }
            }

            // copy to store
            cPriceB_ = cP;
            fPriceB_ = fP;

            cfMaturityTimes_ = new List<double>();
            for (var i = 0; i < cfMaturities_.Count; i++)
            {
                cfMaturityTimes_.Add(timeFromReference(cpiOptionDateFromTenor(cfMaturities_[i])));
            }

            capPrice_ = interpolator2d_.interpolate(cfMaturityTimes_, cfMaturityTimes_.Count,
                allStrikes_, allStrikes_.Count, cPriceB_);

            capPrice_.enableExtrapolation();

            floorPrice_ = interpolator2d_.interpolate(cfMaturityTimes_, cfMaturityTimes_.Count,
                allStrikes_, allStrikes_.Count, fPriceB_);

            floorPrice_.enableExtrapolation();
        }
    }
}
