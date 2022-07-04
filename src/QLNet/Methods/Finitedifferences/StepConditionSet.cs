using System.Collections.Generic;
using QLNet.Math;

namespace QLNet.Methods.Finitedifferences
{
    [JetBrains.Annotations.PublicAPI] public class StepConditionSet<array_type> : List<IStepCondition<array_type>>, IStepCondition<array_type>
        where array_type : Vector
    {
        public void applyTo(object o, double t)
        {
            var a = (List<array_type>)o;
            for (var i = 0; i < Count; i++)
            {
                this[i].applyTo(a[i], t);
            }
        }
    }
}