using JetBrains.Annotations;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public interface ISchemeFactory
    {
        IMixedScheme factory(object L, object bcs, object[] additionalInputs = null);
    }
}
