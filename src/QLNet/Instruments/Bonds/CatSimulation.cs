using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    public abstract class CatSimulation
    {
        protected Date end_;
        protected Date start_;

        protected CatSimulation(Date start, Date end)
        {
            start_ = start;
            end_ = end;
        }

        public abstract bool nextPath(List<KeyValuePair<Date, double>> path);
    }
}
