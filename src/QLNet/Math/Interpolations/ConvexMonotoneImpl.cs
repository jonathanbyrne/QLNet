using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Extensions;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ConvexMonotoneImpl : Interpolation.templateImpl
    {
        public enum SectionType
        {
            EverywhereConstant,
            ConstantGradient,
            QuadraticMinimum,
            QuadraticMaximum
        }

        private ISectionHelper extrapolationHelper_;
        private bool forcePositive_, constantLastPeriod_;
        private double monotonicity_;
        private Dictionary<double, ISectionHelper> preSectionHelpers_ = new Dictionary<double, ISectionHelper>();
        private double quadraticity_;
        private Dictionary<double, ISectionHelper> sectionHelpers_ = new Dictionary<double, ISectionHelper>();

        public ConvexMonotoneImpl(List<double> xBegin, int size, List<double> yBegin,
            double quadraticity, double monotonicity, bool forcePositive, bool constantLastPeriod,
            Dictionary<double, ISectionHelper> preExistingHelpers)
            : base(xBegin, size, yBegin)
        {
            preSectionHelpers_ = preExistingHelpers;
            forcePositive_ = forcePositive;
            constantLastPeriod_ = constantLastPeriod;
            quadraticity_ = quadraticity;
            monotonicity_ = monotonicity;

            Utils.QL_REQUIRE(monotonicity_ >= 0 && monotonicity_ <= 1, () => "Monotonicity must lie between 0 and 1");
            Utils.QL_REQUIRE(quadraticity_ >= 0 && quadraticity_ <= 1, () => "Quadraticity must lie between 0 and 1");
            Utils.QL_REQUIRE(size_ >= 2, () => "Single point provided, not supported by convex " +
                                               "monotone method as first point is ignored");
            Utils.QL_REQUIRE(size_ - preExistingHelpers.Count > 1, () => "Too many existing helpers have been supplied");
        }

        public override double derivative(double x) => throw new NotImplementedException("Convex-monotone spline derivative not implemented");

        public Dictionary<double, ISectionHelper> getExistingHelpers()
        {
            var retArray = new Dictionary<double, ISectionHelper>(sectionHelpers_);
            if (constantLastPeriod_)
            {
                retArray.Remove(retArray.Keys.Last());
            }

            return retArray;
        }

        public override double primitive(double x)
        {
            if (x >= xBegin_.Last())
            {
                return extrapolationHelper_.primitive(x);
            }

            double i;
            if (x >= sectionHelpers_.Keys.Last())
            {
                i = sectionHelpers_.Keys.Last();
            }
            else if (x <= sectionHelpers_.Keys.First())
            {
                i = sectionHelpers_.Keys.First();
            }
            else
            {
                i = sectionHelpers_.Keys.First(y => x < y);
            }

            return sectionHelpers_[i].primitive(x);
        }

        public override double secondDerivative(double x) => throw new NotImplementedException("Convex-monotone spline second derivative not implemented");

        public override void update()
        {
            sectionHelpers_.Clear();
            if (size_ == 2) //single period
            {
                ISectionHelper singleHelper = new EverywhereConstantHelper(yBegin_[1], 0.0, xBegin_[0]);
                sectionHelpers_.Add(xBegin_[1], singleHelper);
                extrapolationHelper_ = singleHelper;
                return;
            }

            List<double> f = new InitializedList<double>(size_);
            sectionHelpers_ = new Dictionary<double, ISectionHelper>(preSectionHelpers_);
            var startPoint = sectionHelpers_.Count + 1;

            //first derive the boundary forwards.
            for (var i = startPoint; i < size_ - 1; ++i)
            {
                var dxPrev = xBegin_[i] - xBegin_[i - 1];
                var dx = xBegin_[i + 1] - xBegin_[i];
                f[i] = dxPrev / (dx + dxPrev) * yBegin_[i]
                       + dx / (dx + dxPrev) * yBegin_[i + 1];
            }

            if (startPoint > 1)
            {
                f[startPoint - 1] = preSectionHelpers_.Last().Value.fNext();
            }

            if (startPoint == 1)
            {
                f[0] = 1.5 * yBegin_[1] - 0.5 * f[1];
            }

            f[size_ - 1] = 1.5 * yBegin_[size_ - 1] - 0.5 * f[size_ - 2];

            if (forcePositive_)
            {
                if (f[0] < 0)
                {
                    f[0] = 0.0;
                }

                if (f[size_ - 1] < 0.0)
                {
                    f[size_ - 1] = 0.0;
                }
            }

            var primitive = 0.0;
            for (var i = 0; i < startPoint - 1; ++i)
            {
                primitive += yBegin_[i + 1] * (xBegin_[i + 1] - xBegin_[i]);
            }

            var endPoint = size_;
            if (constantLastPeriod_)
            {
                endPoint = endPoint - 1;
            }

            for (var i = startPoint; i < endPoint; ++i)
            {
                var gPrev = f[i - 1] - yBegin_[i];
                var gNext = f[i] - yBegin_[i];
                //first deal with the zero gradient case
                if (System.Math.Abs(gPrev) < 1.0E-14 && System.Math.Abs(gNext) < 1.0E-14)
                {
                    ISectionHelper singleHelper = new ConstantGradHelper(f[i - 1], primitive,
                        xBegin_[i - 1],
                        xBegin_[i],
                        f[i]);
                    sectionHelpers_.Add(xBegin_[i], singleHelper);
                }
                else
                {
                    var quadraticity = quadraticity_;
                    ISectionHelper quadraticHelper = null;
                    ISectionHelper convMonotoneHelper = null;
                    if (quadraticity_ > 0.0)
                    {
                        if (gPrev >= -2.0 * gNext && gPrev > -0.5 * gNext && forcePositive_)
                        {
                            quadraticHelper = new QuadraticMinHelper(xBegin_[i - 1],
                                xBegin_[i],
                                f[i - 1], f[i],
                                yBegin_[i],
                                primitive);
                        }
                        else
                        {
                            quadraticHelper = new QuadraticHelper(xBegin_[i - 1],
                                xBegin_[i],
                                f[i - 1], f[i],
                                yBegin_[i],
                                primitive);
                        }
                    }

                    if (quadraticity_ < 1.0)
                    {
                        if (gPrev > 0.0 && -0.5 * gPrev >= gNext && gNext >= -2.0 * gPrev ||
                            gPrev < 0.0 && -0.5 * gPrev <= gNext && gNext <= -2.0 * gPrev)
                        {
                            quadraticity = 1.0;
                            if (quadraticity_.IsEqual(0.0))
                            {
                                if (forcePositive_)
                                {
                                    quadraticHelper = new QuadraticMinHelper(
                                        xBegin_[i - 1],
                                        xBegin_[i],
                                        f[i - 1], f[i],
                                        yBegin_[i],
                                        primitive);
                                }
                                else
                                {
                                    quadraticHelper = new QuadraticHelper(
                                        xBegin_[i - 1],
                                        xBegin_[i],
                                        f[i - 1], f[i],
                                        yBegin_[i],
                                        primitive);
                                }
                            }
                        }
                        else if (gPrev < 0.0 && gNext > -2.0 * gPrev ||
                                 gPrev > 0.0 && gNext < -2.0 * gPrev)
                        {
                            var eta = (gNext + 2.0 * gPrev) / (gNext - gPrev);
                            var b2 = (1.0 + monotonicity_) / 2.0;
                            if (eta < b2)
                            {
                                convMonotoneHelper = new ConvexMonotone2Helper(
                                    xBegin_[i - 1],
                                    xBegin_[i],
                                    gPrev, gNext,
                                    yBegin_[i],
                                    eta, primitive);
                            }
                            else
                            {
                                if (forcePositive_)
                                {
                                    convMonotoneHelper = new ConvexMonotone4MinHelper(
                                        xBegin_[i - 1],
                                        xBegin_[i],
                                        gPrev, gNext,
                                        yBegin_[i],
                                        b2, primitive);
                                }
                                else
                                {
                                    convMonotoneHelper = new ConvexMonotone4Helper(
                                        xBegin_[i - 1],
                                        xBegin_[i],
                                        gPrev, gNext,
                                        yBegin_[i],
                                        b2, primitive);
                                }
                            }
                        }
                        else if (gPrev > 0.0 && gNext < 0.0 && gNext > -0.5 * gPrev ||
                                 gPrev < 0.0 && gNext > 0.0 && gNext < -0.5 * gPrev)
                        {
                            var eta = gNext / (gNext - gPrev) * 3.0;
                            var b3 = (1.0 - monotonicity_) / 2.0;
                            if (eta > b3)
                            {
                                convMonotoneHelper = new ConvexMonotone3Helper(
                                    xBegin_[i - 1],
                                    xBegin_[i],
                                    gPrev, gNext,
                                    yBegin_[i],
                                    eta, primitive);
                            }
                            else
                            {
                                if (forcePositive_)
                                {
                                    convMonotoneHelper = new ConvexMonotone4MinHelper(
                                        xBegin_[i - 1],
                                        xBegin_[i],
                                        gPrev, gNext,
                                        yBegin_[i],
                                        b3, primitive);
                                }
                                else
                                {
                                    convMonotoneHelper = new ConvexMonotone4Helper(
                                        xBegin_[i - 1],
                                        xBegin_[i],
                                        gPrev, gNext,
                                        yBegin_[i],
                                        b3, primitive);
                                }
                            }
                        }
                        else
                        {
                            var eta = gNext / (gPrev + gNext);
                            var b2 = (1.0 + monotonicity_) / 2.0;
                            var b3 = (1.0 - monotonicity_) / 2.0;
                            if (eta > b2)
                            {
                                eta = b2;
                            }

                            if (eta < b3)
                            {
                                eta = b3;
                            }

                            if (forcePositive_)
                            {
                                convMonotoneHelper = new ConvexMonotone4MinHelper(
                                    xBegin_[i - 1],
                                    xBegin_[i],
                                    gPrev, gNext,
                                    yBegin_[i],
                                    eta, primitive);
                            }
                            else
                            {
                                convMonotoneHelper = new ConvexMonotone4Helper(
                                    xBegin_[i - 1],
                                    xBegin_[i],
                                    gPrev, gNext,
                                    yBegin_[i],
                                    eta, primitive);
                            }
                        }
                    }

                    if (quadraticity.IsEqual(1.0))
                    {
                        sectionHelpers_.Add(xBegin_[i], quadraticHelper);
                    }
                    else if (quadraticity.IsEqual(0.0))
                    {
                        sectionHelpers_.Add(xBegin_[i], convMonotoneHelper);
                    }
                    else
                    {
                        sectionHelpers_.Add(xBegin_[i], new ComboHelper(quadraticHelper, convMonotoneHelper, quadraticity));
                    }
                }

                primitive += yBegin_[i] * (xBegin_[i] - xBegin_[i - 1]);
            }

            if (constantLastPeriod_)
            {
                sectionHelpers_.Add(xBegin_[size_ - 1], new EverywhereConstantHelper(yBegin_[size_ - 1], primitive, xBegin_[size_ - 2]));
                extrapolationHelper_ = sectionHelpers_[xBegin_[size_ - 1]];
            }
            else
            {
                extrapolationHelper_ = new EverywhereConstantHelper(sectionHelpers_.Last().Value.value(xBegin_.Last()),
                    primitive, xBegin_.Last());
            }
        }

        public override double value(double x)
        {
            if (x >= xBegin_.Last())
            {
                return extrapolationHelper_.value(x);
            }

            double i;
            if (x > sectionHelpers_.Keys.Last())
            {
                i = sectionHelpers_.Keys.Last();
            }
            else if (x < sectionHelpers_.Keys.First())
            {
                i = sectionHelpers_.Keys.First();
            }
            else
            {
                i = sectionHelpers_.Keys.First(y => x < y);
            }

            return sectionHelpers_[i].value(x);
        }
    }
}
