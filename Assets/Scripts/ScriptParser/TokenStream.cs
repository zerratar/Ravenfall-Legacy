using System.Collections;
using System.Collections.Generic;

namespace ZerraBot.Core.ScriptParser
{
    public class TokenStream : IEnumerable<Token>
    {
        private readonly IReadOnlyList<Token> tokens;
        private int index = 0;

        public TokenStream(IReadOnlyList<Token> tokens)
        {
            this.tokens = tokens;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            return tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Token this[int index] => tokens[index];

        public int Count => tokens.Count;

        public bool EndOfStream => index >= tokens.Count - 1;

        public bool TokensAvailable => index < tokens.Count;

        public Token Current => EndOfStream ? null : tokens[index];

        public void ConsumeOptional(TokenType type)
        {
            if (!TokensAvailable) return;
            var next = tokens[index];
            if (next.Type == type)
            {
                Next();
            }
        }

        public Token Consume(string value)
        {
            if (index >= tokens.Count)
            {
                throw new ParserException($"Expected token value '{value}' but no more tokens available.");
            }

            var next = tokens[index];
            if (next.Value == value)
            {
                return Next();
            }

            var pos = next.GetSourcePosition();
            throw new ParserException($"Expected token '{value}' but found '{next.Value}' found. At line {pos.Row}, column {pos.Column}");
        }

        public Token Consume()
        {
            if (index >= tokens.Count)
            {
                throw new ParserException($"No more tokens available.");
            }

            return Next();
        }

        public Token Consume(TokenType type)
        {
            if (index >= tokens.Count)
            {
                throw new ParserException($"Expected token '{type.ToString()}' but no more tokens available.");
            }

            var next = tokens[index];
            if (next.Type == type)
            {
                return Next();
            }

            var pos = next.GetSourcePosition();
            throw new ParserException($"Expected token '{type.ToString()}' but found '{next.Type}' found. At line {pos.Row}, column {pos.Column}");
        }

        public bool NextIs(string value)
        {
            var next = Peek();
            if (next == null) return false;
            return next.Value == value;
        }

        public bool NextIs(TokenType type)
        {
            var next = Peek();
            if (next == null) return false;
            return next.Type == type;
        }

        public bool CurrentIs(TokenType type)
        {
            return Current.Type == type;
        }

        public Token Peek(int offset = 0)
        {
            if (index + offset >= tokens.Count)
            {
                return null;
            }

            return tokens[index + offset];
        }

        public Token Next()
        {
            if (index >= tokens.Count)
            {
                throw new ParserException($"End of stream, no more tokens to read.");
            }

            return tokens[index++];
        }


        public Token Skip(int count)
        {
            if (index + 1 + count >= tokens.Count)
            {
                throw new ParserException($"End of stream, no more tokens to read.");
            }

            index += count;
            return tokens[index];
        }
    }
}