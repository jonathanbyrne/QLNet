using System;
using JetBrains.Annotations;

namespace QLNet.Exceptions
{
    [PublicAPI]
    public class RootNotBracketException : Exception
    {
        public RootNotBracketException()
        {
        }

        public RootNotBracketException(string message)
            : base(message)
        {
        }

        public RootNotBracketException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
