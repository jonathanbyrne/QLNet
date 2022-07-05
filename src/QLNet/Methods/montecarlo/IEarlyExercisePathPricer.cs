using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Methods.montecarlo
{
    [PublicAPI]
    public interface IEarlyExercisePathPricer<PathType, StateType> where PathType : IPath
    {
        List<Func<StateType, double>> basisSystem();

        StateType state(PathType path, int t);

        double value(PathType path, int t);
    }
}
