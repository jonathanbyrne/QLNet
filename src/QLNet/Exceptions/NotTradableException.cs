using System;

namespace QLNet.Exceptions
{
    [JetBrains.Annotations.PublicAPI] public class NotTradableException : Exception
    {
        public NotTradableException()
        {
        }

        public NotTradableException(string message)
           : base(message)
        {
        }

        public NotTradableException(string message, Exception inner)
           : base(message, inner)
        {
        }
    }
}
