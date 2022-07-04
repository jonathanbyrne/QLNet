using System.Collections.Generic;
using System.Linq;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class NotionalPath
    {
        public NotionalPath()
        {
            var previous = 1.0;//full notional at the beginning
            notionalRate_ = new List<KeyValuePair<Date, double>> { new KeyValuePair<Date, double>(new Date(), previous) };
        }
        public double notionalRate(Date date)  //The fraction of the original notional left on a given date
        {
            var i = 0;
            for (; i < notionalRate_.Count && notionalRate_[i].Key <= date; ++i) //TODO do we take notional after reductions or before?
            { }
            return notionalRate_[i - 1].Value;

        }

        public void reset()
        {
            notionalRate_ = new InitializedList<KeyValuePair<Date, double>>(1, new KeyValuePair<Date, double>(new Date(), 1));
        }

        public void addReduction(Date date, double newRate)
        {
            notionalRate_.Add(new KeyValuePair<Date, double>(date, newRate));
        }

        public double loss() => 1.0 - notionalRate_.Last().Value;

        private List<KeyValuePair<Date, double>> notionalRate_;
    }
}