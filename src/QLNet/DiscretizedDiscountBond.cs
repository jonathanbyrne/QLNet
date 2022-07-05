using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet
{
    [PublicAPI]
    public class DiscretizedDiscountBond : DiscretizedAsset
    {
        public override List<double> mandatoryTimes() => new Vector();

        public override void reset(int size)
        {
            values_ = new Vector(size, 1.0);
        }
    }
}
