﻿using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Models.Shortrate.Onefactormodels
{
    [PublicAPI]
    public class Helper : ISolver1d
    {
        private double discountBondPrice_;
        private double dt_;
        private int size_;
        private Vector statePrices_;
        private double xMin_, dx_;

        public Helper(int i, double xMin, double dx,
            double discountBondPrice,
            OneFactorModel.ShortRateTree tree)
        {
            size_ = tree.size(i);
            dt_ = tree.timeGrid().dt(i);
            xMin_ = xMin;
            dx_ = dx;
            statePrices_ = tree.statePrices(i);
            discountBondPrice_ = discountBondPrice;
        }

        public override double value(double theta)
        {
            var value = discountBondPrice_;
            var x = xMin_;
            for (var j = 0; j < size_; j++)
            {
                var discount = System.Math.Exp(-System.Math.Exp(theta + x) * dt_);
                value -= statePrices_[j] * discount;
                x += dx_;
            }

            return value;
        }
    }
}
