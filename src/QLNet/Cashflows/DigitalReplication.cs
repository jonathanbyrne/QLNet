using JetBrains.Annotations;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class DigitalReplication
    {
        private double gap_;
        private Replication.Type replicationType_;

        public DigitalReplication(Replication.Type t = Replication.Type.Central, double gap = 1e-4)
        {
            gap_ = gap;
            replicationType_ = t;
        }

        public double gap() => gap_;

        public Replication.Type replicationType() => replicationType_;
    }
}
