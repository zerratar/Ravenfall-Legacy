using JetBrains.Annotations;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;

namespace RavenNest.SDK
{
    public class UnityLogger : ILogger
    {
        public void WriteDebug(string message)
        {
            Shinobytes.Debug.Log(message);
        }

        public void WriteError(string message)
        {
            Shinobytes.Debug.LogError(message);
        }

        public void WriteWarning(string message)
        {
            Shinobytes.Debug.LogWarning(message);
        }
        public void Write(string message)
        {
            Shinobytes.Debug.Log(message);
        }

        public void WriteMessage(string message)
        {
            Shinobytes.Debug.Log(message);
        }
    }
}

namespace Shinobytes
{

    public static class Debug
    {
        private static readonly SyntaxHighlightedConsoleLogger console = new SyntaxHighlightedConsoleLogger();

        private static volatile bool patched;

        private static void PatchIfNecessary()
        {
            if (patched) return;

            var s_logger = UnityEngine.Debug.unityLogger;
            var s_loggerField = typeof(UnityEngine.Debug).GetField("s_Logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            s_loggerField.SetValue(null, new PatchedUnityLogger(s_logger));

            patched = true;
        }


        public static void Log(string message)
        {
            PatchIfNecessary();
            var date = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            UnityEngine.Debug.Log(date + message);
            console.WriteLine(date + message);
        }

        public static void Log(object message)
        {
            PatchIfNecessary();
            var date = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            UnityEngine.Debug.Log(date + message);
            console.WriteLine(date + message);
        }

        public static void LogWarning(string message)
        {

            PatchIfNecessary();
            var date = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            UnityEngine.Debug.LogWarning(date + message);
            console.WriteLine(date + " @yel@[WRN] #yel#@bla@" + message);
        }

        public static void LogError(string message)
        {
            PatchIfNecessary();
            var date = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            UnityEngine.Debug.LogError(date + message);
            console.WriteLine(date + " @red@[ERR] " + message);
        }

        public static void LogError(Exception message)
        {
            PatchIfNecessary();
            var date = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            UnityEngine.Debug.LogError(date + message.Message);
            console.WriteLine(date + "@red@[EXC] #red#@bla@" + message.Message);
        }

        public class PatchedUnityLogger : UnityEngine.ILogger
        {
            private UnityEngine.ILogger logger;

            public PatchedUnityLogger(UnityEngine.ILogger logger)
            {
                this.logger = logger;
            }

            public UnityEngine.ILogHandler logHandler { get => logger.logHandler; set => logger.logHandler = value; }
            public bool logEnabled { get => logger.logEnabled; set => logger.logEnabled = value; }
            public UnityEngine.LogType filterLogType { get => logger.filterLogType; set => logger.filterLogType = value; }

            public bool IsLogTypeAllowed(UnityEngine.LogType logType) => logger.IsLogTypeAllowed(logType);
            public void Log(UnityEngine.LogType logType, object message) => logger.Log(logType, message);

            public void Log(UnityEngine.LogType logType, object message, UnityEngine.Object context) => logger.Log(logType, message, context);

            public void Log(UnityEngine.LogType logType, string tag, object message) => logger.Log(logType, tag, message);
            public void Log(UnityEngine.LogType logType, string tag, object message, UnityEngine.Object context) => logger.Log(logType, tag, message, context);

            public void Log(object message) => logger.Log(message);

            public void Log(string tag, object message) => logger.Log(tag, message);

            public void Log(string tag, object message, UnityEngine.Object context) => logger.Log(tag, message, context);

            public void LogError(string tag, object message) => logger.LogError(tag, message);
            public void LogError(string tag, object message, UnityEngine.Object context) => logger.LogError(tag, message, context);

            public void LogException(Exception exception) => logger.LogException(exception);
            public void LogException(Exception exception, UnityEngine.Object context) => logger.LogException(exception, context);

            public void LogFormat(UnityEngine.LogType logType, string format, params object[] args) => logger.LogFormat(logType, format, args);

            public void LogFormat(UnityEngine.LogType logType, UnityEngine.Object context, string format, params object[] args) => logger.LogFormat(logType, context, format, args);

            public void LogWarning(string tag, object message) => logger.LogWarning(tag, message);

            public void LogWarning(string tag, object message, UnityEngine.Object context) => logger.LogWarning(tag, message, context);
        }
    }

    public class SyntaxHighlightedConsoleLogger
    {
        private readonly object writelock = new object();

        protected static ConsoleColor MSG = ConsoleColor.White;
        protected static ConsoleColor DBG = ConsoleColor.Cyan;
        protected static ConsoleColor WRN = ConsoleColor.Yellow;
        protected static ConsoleColor ERR = ConsoleColor.Red;

        private ConsoleColor[] consoleColors;
        private string[] consoleColorNames;
        private readonly Dictionary<string, ConsoleColor> colorCache = new Dictionary<string, ConsoleColor>();

        internal static readonly Dictionary<TextTokenType, ConsoleColor> TokenColors = new Dictionary<TextTokenType, ConsoleColor> {
            { TextTokenType.Number, ConsoleColor.Cyan },
            { TextTokenType.QuoutedString, ConsoleColor.Magenta },
        };

        public SyntaxHighlightedConsoleLogger()
        {
            Console.OutputEncoding = Encoding.Unicode;

            this.consoleColors = Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>().ToArray();
            this.consoleColorNames = Enum.GetNames(typeof(ConsoleColor));
        }

        public void Write(string message) => WriteOperations(ParseMessageOperations(message));

        public void WriteLine(string message) => WriteLineOperations(ParseMessageOperations(message));

        private void WriteLineOperations(IReadOnlyList<ConsoleWriteOperation> operations) => WriteOperations(operations, true);

        private void WriteOperations(IReadOnlyList<ConsoleWriteOperation> operations, bool newLine = false)
        {
            lock (writelock)
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
                if (newLine)
                {
                    Console.WriteLine();
                }
            }
        }
        internal IReadOnlyList<ConsoleWriteOperation> ParseMessageOperations(string message)
        {
            var ops = new List<ConsoleWriteOperation>();
            var tokens = Tokenize(message);
            var tokenIndex = 0;
            var colorOverride = false;
            var foregroundColor = Console.ForegroundColor;
            var backgroundColor = Console.BackgroundColor;
            while (tokenIndex < tokens.Count)
            {
                var token = tokens[tokenIndex];
                switch (token.Type)
                {
                    case TextTokenType.At:
                        {
                            if (tokenIndex > 0)
                            {
                                var prev = tokens[tokenIndex - 1];
                                var prevOp = ops[ops.Count - 1];
                                if (prev.Text.EndsWith("\\"))
                                {
                                    ops[ops.Count - 1] = new ConsoleWriteOperation(prevOp.Text.Remove(prevOp.Text.Length - 1), prevOp.ForegroundColor, prevOp.BackgroundColor);
                                    goto default;
                                }
                            }
                            colorOverride = true;
                            foregroundColor = ParseColor(tokens[++tokenIndex].Text, ConsoleColorType.Foreground);
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
                            backgroundColor = ParseColor(tokens[++tokenIndex].Text, ConsoleColorType.Background);
                            ++tokenIndex;// var endToken = tokens[++tokenIndex];                            
                        }
                        break;

                    //case TextTokenType.Number:
                    //    ops.Add(new ConsoleWriteOperation(token.Text, ConsoleColor.Blue, backgroundColor));
                    //    break;

                    case TextTokenType.Number:
                        {
                            // check if this is a date or time otherwise jump to default.
                            // since we don't color spaces, there is no need to check for a "datetime"
                            var tempText = token.Text;
                            var tempIndex = tokenIndex;
                            while (tempIndex + 1 < tokens.Count)
                            {
                                var next = tokens[++tempIndex];

                                // Time separator
                                if (next.Text == ":")
                                {
                                    var b = tokens[tempIndex + 1];
                                    if (b.Type != TextTokenType.Number)
                                    {
                                        break;
                                    }
                                }

                                // Date Separator
                                else if (next.Type == TextTokenType.Dash)
                                {
                                    var b = tokens[tempIndex + 1];
                                    if (b.Type != TextTokenType.Number)
                                    {
                                        break;
                                    }
                                }
                                else if (next.Type != TextTokenType.Number)
                                {
                                    break;
                                }
                                tempText += next.Text;
                            }

                            if (tempText != token.Text)
                            {
                                ops.Add(new ConsoleWriteOperation(tempText, !colorOverride && TokenColors.TryGetValue(token.Type, out var c) ? c : foregroundColor, backgroundColor));
                                tokenIndex = tempIndex - 1;
                                break;
                            }
                        }
                        goto default;

                    default:
                        {
                            ops.Add(new ConsoleWriteOperation(token.Text, !colorOverride && TokenColors.TryGetValue(token.Type, out var color) ? color : foregroundColor, backgroundColor));
                        }
                        break;
                }
                tokenIndex++;
            }
            return ops;
        }

        internal ConsoleColor ParseColor(string color, ConsoleColorType consoleColorType)
        {
            var key = color.ToLower();

            var fgColor = Console.ForegroundColor;
            var bgColor = Console.BackgroundColor;

            if (key == "clear" || key == "reset" || key == "null" || key == "empty" || key == "none" || key == "default")
            {
                Console.ResetColor();
                if (consoleColorType == ConsoleColorType.Foreground)
                {
                    Console.BackgroundColor = bgColor;
                    return Console.ForegroundColor;
                }
                else
                {
                    Console.ForegroundColor = fgColor;
                    return Console.BackgroundColor;
                }
            }

            var output = consoleColorType == ConsoleColorType.Foreground ? fgColor : bgColor;
            if (colorCache.TryGetValue(key, out var v))
            {
                return v;
            }

            if (int.TryParse(color, out var value))
            {
                output = (ConsoleColor)value;
            }
            else
            {
                // ex: @white@
                var names = consoleColorNames;
                var possibleColorName = names.FirstOrDefault(x => x.Equals(color, StringComparison.OrdinalIgnoreCase));
                if (possibleColorName != null)
                {
                    output = consoleColors[Array.IndexOf(names, possibleColorName)];
                }
                else
                {
                    // ex: @whi@
                    possibleColorName = names.FirstOrDefault(x => x.StartsWith(color, StringComparison.OrdinalIgnoreCase));
                    if (possibleColorName != null)
                    {
                        output = consoleColors[Array.IndexOf(names, possibleColorName)];
                    }
                }
            }

            return colorCache[key] = output;
        }

        internal IReadOnlyList<TextToken> Tokenize(string message)
        {
            var tokens = new List<TextToken>();
            var index = 0;

            var badText = new HashSet<char>("1234567890'\"@(){}[]#".ToArray());

            // reads the tokens until the condition is met.
            string ReadUntilCondition(Func<char, bool> condition)
            {
                var token = message[index];
                var str = token.ToString();
                while (index + 1 < message.Length)
                {
                    var next = message[index + 1];
                    if (condition(next)) break;
                    str += message[++index];
                }
                return str;
            }

            // reads the tokens while the condition is met/returns true.
            string ReadWhile(Func<char, bool> condition)
            {
                return ReadUntilCondition(next => !condition(next));
            }

            // reads the token until we find the expected character.
            string ReadUntil(char expectedEnding)
            {
                return ReadUntilCondition(next => next == expectedEnding);
            }

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

                    case '.':
                        tokens.Add(new TextToken(TextTokenType.Dot, "."));
                        break;

                    case '-':
                        tokens.Add(new TextToken(TextTokenType.Dash, "-"));
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        {
                            var str = ReadUntilCondition(x => !char.IsDigit(x));

                            // check if next is . and proceeded by a number. If so, this is most likely a floating point number.
                            // even if we have string that represents a IP or similar; 127.0.0.1 this could be fair assumption.
                            // although, as for automatic coloring IPs and Numbers will technically be different.

                            // peek into the future, we need to know the result of the upcoming
                            // two tokens to determine if this is a floating point or not.

                            if (index + 2 < message.Length)
                            {
                                var a = message[index + 1];
                                var b = message[index + 2];
                                if (a == '.' && char.IsDigit(b))
                                {
                                    ++index;
                                    str += ReadUntilCondition(x => !char.IsDigit(x));
                                }
                            }

                            tokens.Add(new TextToken(TextTokenType.Number, str));
                        }
                        break;

                    case '\'':
                    case '"':
                        {
                            var expectedEnding = token == '"' ? '"' : '\'';
                            var str = ReadUntil(expectedEnding);
                            str += expectedEnding;
                            ++index;
                            tokens.Add(new TextToken(TextTokenType.QuoutedString, str));
                        }
                        break;

                    case '[':
                        tokens.Add(new TextToken(TextTokenType.OpenBracket, "["));
                        break;

                    case ']':
                        tokens.Add(new TextToken(TextTokenType.CloseBracket, "]"));
                        break;


                    case '{':
                        tokens.Add(new TextToken(TextTokenType.OpenCurlyBracket, "["));
                        break;

                    case '}':
                        tokens.Add(new TextToken(TextTokenType.CloseCurlyBracket, "]"));
                        break;

                    case '(':
                        tokens.Add(new TextToken(TextTokenType.OpenParen, "("));
                        break;

                    case ')':
                        tokens.Add(new TextToken(TextTokenType.CloseParen, ")"));
                        break;
                    default:
                        {
                            var str = ReadUntilCondition(x => badText.Contains(char.ToLower(x)));
                            //var str = ReadUntil('@');
                            tokens.Add(new TextToken(TextTokenType.Text, str));
                            break;
                        }
                }

                index++;
            }
            return tokens;

        }

        internal struct TextToken
        {
            public readonly string Text;
            public readonly TextTokenType Type;

            public TextToken(TextTokenType type, string text)
            {
                Type = type;
                Text = text;
            }

            public override string ToString()
            {
                return "TextToken " + Type + ": " + Text;
            }
        }

        internal enum TextTokenType
        {
            At, Dot, Dash, Hash, Text, QuoutedString, Number, OpenBracket, CloseBracket, OpenCurlyBracket, CloseCurlyBracket, OpenParen, CloseParen,
        }

        internal struct ConsoleWriteOperation
        {
            public readonly string Text;
            public readonly ConsoleColor ForegroundColor;
            public readonly ConsoleColor BackgroundColor;

            public ConsoleWriteOperation(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
            {
                Text = text;
                ForegroundColor = foregroundColor;
                BackgroundColor = backgroundColor;
            }
        }

        internal struct LoggerScope : IDisposable
        {
            public void Dispose() { }
        }

        internal enum ConsoleColorType
        {
            Foreground,
            Background
        }
    }
}
