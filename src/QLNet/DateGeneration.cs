namespace QLNet
{
    public struct DateGeneration
    {
        public enum Rule
        {
            Backward, /*!< Backward from termination date to effective date. */
            Forward, /*!< Forward from effective date to termination date. */
            Zero, /*!< No intermediate dates between effective date and termination date. */
            ThirdWednesday, /*!< All dates but effective date and termination date are taken to be on the third wednesday of their month*/
            Twentieth, /*!< All dates but the effective date are taken to be the twentieth of their
                              month (used for CDS schedules in emerging markets.)  The termination
                              date is also modified. */
            TwentiethIMM, /*!< All dates but the effective date are taken to be the twentieth of an IMM
                              month (used for CDS schedules.)  The termination date is also modified. */
            OldCDS, /*!< Same as TwentiethIMM with unrestricted date ends and log/short stub
                              coupon period (old CDS convention). */
            CDS, /*!< Credit derivatives standard rule since 'Big Bang' changes in 2009.  */
            CDS2015 /*!< Credit derivatives standard rule since December 20th, 2015.  */
        }
    }
}
