using System;

namespace ZerraBot.Core.ScriptParser
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        {
        }
    }
}