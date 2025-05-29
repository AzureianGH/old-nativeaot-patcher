// This code is licensed under MIT license (see LICENSE for details)

using System;

namespace EarlyBird
{
    internal class NotImplementedException : Exception
    {
        public NotImplementedException()
        {
        }

        public NotImplementedException(string str) : base(str)
        {
        }
    }
}