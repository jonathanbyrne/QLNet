using System;
using System.Collections.Generic;

namespace QLNet.Methods.montecarlo
{
    [JetBrains.Annotations.PublicAPI] public interface IEarlyExercisePathPricer<PathType, StateType> where PathType : IPath
    {
        double value(PathType path, int t);

        StateType state(PathType path, int t);
        List<Func<StateType, double>> basisSystem();
    }
}