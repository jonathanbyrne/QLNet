using System;

namespace QLNet.Exceptions
{
    [JetBrains.Annotations.PublicAPI] public class RootNotBracketException : Exception
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
