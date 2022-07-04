namespace QLNet.Termstructures
{
    public struct Pillar
    {
        //! Enumeration for pillar determination alternatives
        /*! These alternatives specify the determination of the pillar date. */
        public enum Choice
        {
            MaturityDate,     //! instruments maturity date
            LastRelevantDate, //! last date relevant for instrument pricing
            CustomDate        //! custom choice
        }
    }
}