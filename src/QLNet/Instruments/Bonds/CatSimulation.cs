using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    public abstract class CatSimulation
    {
        protected CatSimulation(Date start, Date end)
        {
            start_ = start;
            end_ = end;
        }

        public abstract bool nextPath(List<KeyValuePair<Date, double>> path);

        protected Date start_;
        protected Date end_;
    }
}