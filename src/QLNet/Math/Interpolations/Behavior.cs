namespace QLNet.Math.Interpolations
{
    public enum Behavior
    {
        ShareRanges, /*!< Define both interpolations over the
                               whole range defined by the passed
                               iterators. This is the default
                               behavior. */
        SplitRanges /*!< Define the first interpolation over the
                               first part of the range, and the second
                               interpolation over the second part. */
    }
}
