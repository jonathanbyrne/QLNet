namespace QLNet.Methods.montecarlo
{
    [JetBrains.Annotations.PublicAPI] public interface IPathGenerator<GSG>
    {
        Sample<IPath> next();
        Sample<IPath> antithetic();
    }
}