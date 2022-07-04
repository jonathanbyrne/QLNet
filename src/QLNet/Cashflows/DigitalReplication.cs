namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class DigitalReplication
    {
        private double gap_;
        private Replication.Type replicationType_;

        public DigitalReplication(Replication.Type t = Replication.Type.Central, double gap = 1e-4)
        {
            gap_ = gap;
            replicationType_ = t;
        }

        public Replication.Type replicationType() => replicationType_;

        public double gap() => gap_;
    }
}