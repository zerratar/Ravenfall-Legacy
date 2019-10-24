using System.Collections.Concurrent;

namespace ZerraBot.Core.ScriptParser
{
    internal class LexerContext
    {
        private readonly ConcurrentDictionary<int, SourceInfo> sourcePositions
            = new ConcurrentDictionary<int, SourceInfo>();

        private readonly ConcurrentDictionary<int, Range> rowRanges
            = new ConcurrentDictionary<int, Range>();

        public LexerContext(string fileName, string sourceCode)
        {
            FileName = fileName;
            SourceCode = sourceCode;
            PrepareSourcePositions();
        }

        public string SourceCode { get; }
        public string FileName { get; }
        public SourceInfo GetSourcePosition(int index)
        {
            if (sourcePositions.TryGetValue(index, out var val))
            {
                return val;
            }

            var rowIndex = 0;
            var colIndex = index;
            foreach (var range in rowRanges)
            {
                if (range.Value.Within(index))
                {
                    rowIndex = range.Key;
                    colIndex = range.Value.GetColumn(index);
                    break;
                }
            }

            return sourcePositions[index] = new SourceInfo(rowIndex + 1, colIndex + 1);
        }

        private void PrepareSourcePositions()
        {
            var start = 0;
            var end = 0;
            var row = 0;
            for (var i = 0; i < SourceCode.Length; ++i)
            {
                var t = SourceCode[i];
                if (t == '\n')
                {
                    end = i;
                    rowRanges[row] = new Range(start, end);
                    ++row;
                    start = i + 1;
                }
            }

            rowRanges[row] = new Range(start, SourceCode.Length - 1);
        }

        private class Range
        {
            public Range(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }
            public int End { get; }

            public bool Within(int index)
            {
                return index >= Start && index <= End;
            }

            public int GetColumn(int index)
            {
                return index - Start;
            }
        }
    }
}