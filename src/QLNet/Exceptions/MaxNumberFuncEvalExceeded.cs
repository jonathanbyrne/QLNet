using System;

namespace QLNet.Exceptions
{
    [JetBrains.Annotations.PublicAPI] public class MaxNumberFuncEvalExceeded : Exception
    {
        public MaxNumberFuncEvalExceeded()
        {
        }

        public MaxNumberFuncEvalExceeded(string message)
           : base(message)
        {
        }

        public MaxNumberFuncEvalExceeded(string message, Exception inner)
           : base(message, inner)
        {
        }
    }
}
