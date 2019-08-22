using System;

namespace LuviKunG.Console
{
    public class LuviConsoleException : Exception
    {
        public LuviConsoleException() { }
        public LuviConsoleException(string message) : base(message) { }
    }
}