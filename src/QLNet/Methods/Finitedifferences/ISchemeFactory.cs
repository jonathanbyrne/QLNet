namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public interface ISchemeFactory
    {
        IMixedScheme factory(object L, object bcs, object[] additionalInputs = null);
    }
}