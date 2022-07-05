using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Methods.Finitedifferences
{
    [PublicAPI]
    public class BoundaryConditionSet : List<List<BoundaryCondition<IOperator>>>
    {
    }
}
