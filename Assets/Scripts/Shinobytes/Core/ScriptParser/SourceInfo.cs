namespace Shinobytes.Core.ScriptParser
{
    public class SourceInfo
    {
        public SourceInfo(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }
    }
}