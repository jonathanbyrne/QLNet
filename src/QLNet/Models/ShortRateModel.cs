namespace QLNet.Models
{
    public abstract class ShortRateModel : CalibratedModel
    {
        protected ShortRateModel(int nArguments) : base(nArguments) { }

        public abstract Lattice tree(TimeGrid t);
    }
}