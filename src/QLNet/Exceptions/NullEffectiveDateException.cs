using System;

namespace QLNet.Exceptions
{
    [JetBrains.Annotations.PublicAPI] public class NullEffectiveDateException : Exception
    {
        public NullEffectiveDateException()
        {
        }

        public NullEffectiveDateException(string message)
           : base(message)
        {
        }

        public NullEffectiveDateException(string message, Exception inner)
           : base(message, inner)
        {
        }
    }
}
