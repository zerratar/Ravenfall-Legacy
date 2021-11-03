using System;

namespace Shinobytes.Core.ScriptParser
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        {
        }
    }
}