using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace RavenNest.SDK
{
    public class ConsoleLogger : ILogger
    {
        private readonly object writelock = new object();

        public ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.Unicode;
        }

        public void Write(string message)
        {
            WriteOperations(ParseMessageOperations(" " + message));
        }

        public void WriteMessage(string message)
        {
            WriteLineOperations(ParseMessageOperations(" " + message));
        }

        public void WriteDebug(string message)
        {
#if DEBUG
            WriteMessage($"[@{ConsoleColor.Cyan}@DBG@{ConsoleColor.Gray}@] {message}");
#endif
        }

        public void WriteWarning(string message)
        {
            WriteMessage($"@{ConsoleColor.Yellow}@{message}");
        }

        public void WriteError(string errorMessage)
        {
            WriteMessage($"@{ConsoleColor.Red}@{errorMessage}");
        }

        private void WriteLineOperations(IReadOnlyList<ConsoleWriteOperation> operations)
        {
            WriteOperations(operations, true);
        }

        private void WriteOperations(IReadOnlyList<ConsoleWriteOperation> operations, bool newLine = false)
        {
            lock (writelock)
            {
                try
                {
                    var prevForeground = Console.ForegroundColor;
                    var prevBackground = Console.BackgroundColor;
                    foreach (var op in operations)
                    {
                        Console.ForegroundColor = op.ForegroundColor;
                        Console.BackgroundColor = op.BackgroundColor;
                        Console.Write(op.Text);
                    }
                    Console.ForegroundColor = prevForeground;
                    Console.BackgroundColor = prevBackground;
                }
                catch (Exception exc)
                {
                    // ignored for now
                }

                if (newLine)
                {
                    Console.WriteLine();
                }
            }
        }

        private IReadOnlyList<ConsoleWriteOperation> ParseMessageOperations(string message)
        {
            var ops = new List<ConsoleWriteOperation>();
            var tokens = Tokenize(message);
            var tokenIndex = 0;

            var foregroundColor = Console.ForegroundColor;
            var backgroundColor = Console.BackgroundColor;
            while (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];
                switch (token.Type)
                {
                    case TextTokenType.At:
                        {
                            var prev = tokens[tokenIndex - 1];
                            var prevOp = ops[ops.Count - 1];
                            if (prev.Text.EndsWith("\\"))
                            {
                                ops[ops.Count - 1] = new ConsoleWriteOperation(prevOp.Text.Remove(prevOp.Text.Length - 1), prevOp.ForegroundColor, prevOp.BackgroundColor);
                                goto default;
                            }
                            foregroundColor = ConsoleWriteOperation.EnsureValidColor(ParseColor(tokens[++tokenIndex].Text), Console.ForegroundColor);
                            ++tokenIndex;// var endToken = tokens[++tokenIndex];                            
                        }
                        break;
                    case TextTokenType.Hash:
                        {
                            var prev = tokens[tokenIndex - 1];
                            var prevOp = ops[ops.Count - 1];
                            if (prev.Text.EndsWith("\\"))
                            {
                                ops[ops.Count - 1] = new ConsoleWriteOperation(prevOp.Text.Remove(prevOp.Text.Length - 1), prevOp.ForegroundColor, prevOp.BackgroundColor);
                                goto default;
                            }
                            backgroundColor = ConsoleWriteOperation.EnsureValidColor(ParseColor(tokens[++tokenIndex].Text), Console.BackgroundColor);
                            ++tokenIndex;// var endToken = tokens[++tokenIndex];                            
                        }
                        break;
                    default:
                        ops.Add(new ConsoleWriteOperation(token.Text, foregroundColor, backgroundColor));
                        break;
                }
                tokenIndex++;
            }
            return ops;
        }

        private static ConsoleColor ParseColor(string color)
        {
            if (int.TryParse(color, out var value))
            {
                return (ConsoleColor)value;
            }

            // ex: @white@
            var names = Enum.GetNames(typeof(ConsoleColor));
            var possibleColorName = names.FirstOrDefault(x => x.Equals(color, StringComparison.OrdinalIgnoreCase));
            if (possibleColorName != null)
            {
                var val = Enum.GetValues(typeof(ConsoleColor))
                    .Cast<ConsoleColor>()
                    .ElementAt(Array.IndexOf(names, possibleColorName));
                return val;
            }

            // ex: @whi@
            possibleColorName = names.FirstOrDefault(x => x.StartsWith(color, StringComparison.OrdinalIgnoreCase));
            if (possibleColorName != null)
            {
                var val = Enum.GetValues(typeof(ConsoleColor))
                    .Cast<ConsoleColor>()
                    .ElementAt(Array.IndexOf(names, possibleColorName));
                return val;
            }

            return Console.ForegroundColor;
        }

        private IReadOnlyList<TextToken> Tokenize(string message)
        {
            var tokens = new List<TextToken>();
            var index = 0;
            while (index < message.Length)
            {
                var token = message[index];
                switch (token)
                {
                    case '@':
                        tokens.Add(new TextToken(TextTokenType.At, "@"));
                        break;
                    case '#':
                        tokens.Add(new TextToken(TextTokenType.Hash, "#"));
                        break;
                    default:
                        {
                            var str = token.ToString();
                            while (index + 1 < message.Length)
                            {
                                var next = message[index + 1];
                                if (next == '@' || next == '#') break;
                                str += message[++index];
                            }
                            tokens.Add(new TextToken(TextTokenType.Text, str));
                            break;
                        }
                }

                index++;
            }
            return tokens;
        }

        private struct TextToken
        {
            public readonly string Text;
            public readonly TextTokenType Type;

            public TextToken(TextTokenType type, string text)
            {
                Type = type;
                Text = text;
            }
        }

        private enum TextTokenType
        {
            At, Hash, Text
        }

        private struct ConsoleWriteOperation
        {
            public readonly string Text;
            public readonly ConsoleColor ForegroundColor;
            public readonly ConsoleColor BackgroundColor;

            public ConsoleWriteOperation(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
            {
                Text = text;
                ForegroundColor = EnsureValidColor(foregroundColor, Console.ForegroundColor);
                BackgroundColor = EnsureValidColor(backgroundColor, Console.BackgroundColor);
            }

            public static ConsoleColor EnsureValidColor(ConsoleColor color, ConsoleColor fallback)
            {
                var i = (int)color;
                if (i < 0 || i > 15)
                {
                    return fallback;
                }
                return color;
            }
        }
    }
}