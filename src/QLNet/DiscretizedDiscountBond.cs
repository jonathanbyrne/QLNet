using System.Collections.Generic;
using QLNet.Math;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class DiscretizedDiscountBond : DiscretizedAsset
    {
        public override void reset(int size)
        {
            values_ = new Vector(size, 1.0);
        }

        public override List<double> mandatoryTimes() => new Vector();
    }
}