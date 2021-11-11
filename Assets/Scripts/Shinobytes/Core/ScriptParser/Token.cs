namespace Shinobytes.Core.ScriptParser
{
    public class Token
    {
        private readonly LexerContext ctx;

        internal Token(LexerContext ctx, int index, TokenType type, string value)
        {
            this.ctx = ctx;
            Index = index;
            Type = type;
            Value = value;
        }

        public int Index { get; }
        public TokenType Type { get; }
        public string Value { get; }

        public SourceInfo GetSourcePosition()
        {
            return ctx.GetSourcePosition(Index);
        }

        public override string ToString()
        {
            var pos = GetSourcePosition();
            return pos.Row + ":" + pos.Column + " [" + Index + "] " + Type + ": " + Value;
        }
    }
}