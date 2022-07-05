using System;
using JetBrains.Annotations;

namespace QLNet.Exceptions
{
    [PublicAPI]
    public class InvalidPriceSignException : Exception
    {
        public InvalidPriceSignException()
        {
        }

        public InvalidPriceSignException(string message)
            : base(message)
        {
        }

        public InvalidPriceSignException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
