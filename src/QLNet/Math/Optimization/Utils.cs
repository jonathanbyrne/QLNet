using System.Collections.Generic;

namespace QLNet.Math.Optimization
{
    public static partial class Utils
    {
        // Computes the size of the simplex
        public static double computeSimplexSize(List<Vector> vertices)
        {
            var center = new Vector(vertices[0].Count, 0);
            for (var i = 0; i < vertices.Count; ++i)
            {
                center += vertices[i];
            }

            center *= 1 / (double)(vertices.Count);
            double result = 0;
            for (var i = 0; i < vertices.Count; ++i)
            {
                var temp = vertices[i] - center;
                result += Vector.Norm2(temp);
            }

            return result / vertices.Count;
        }
    }
}
