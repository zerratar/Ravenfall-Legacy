using System;
using System.Collections.Generic;
using System.Linq;

namespace Shinobytes.Core.ScriptParser
{
    public class Lexer
    {
        public Lexer()
        {
        }

        public TokenStream Tokenize(string content, bool keepWhitespace = false, bool keepComments = false)
        {
            var ctx = new LexerContext(null, content);
            var tokens = new List<Token>();

            var lastIndex = -1;
            var index = 0;

            char Peek(int offset = 1)
            {
                return index + offset >= content.Length ? '\0' : content[index + 1];
            }

            char Next()
            {
                return ++index >= content.Length ? '\0' : content[index];
            }

            void Token(TokenType type, string value = null)
            {
                value = value ?? content[index].ToString();
                tokens.Add(new Token(ctx, index, type, value));
            }

            void ParseSinglelineComment()
            {
                var comment = "";
                var t = content[index];
                var n = Peek();
                if (t != '/' || n != '/')
                {
                    return;
                }

                index += 2;
                t = content[index];
                while (t != '\0' && t != '\n')
                {
                    comment += t;
                    t = Next();
                }
                if (keepComments)
                    Token(TokenType.SinglelineComment, comment);
            }

            void ParseMultilineComment()
            {
                var comment = "";
                var t = content[index];
                var n = Peek();
                if (t != '/' || n != '*')
                {
                    return;
                }
                index += 2;
                t = content[index];
                do
                {
                    n = Peek();
                    if (t == '*' && n == '/') break;
                    comment += t;
                    t = Next();
                } while (true);
                if (keepComments)
                    Token(TokenType.MultilineComment, comment);
            }

            void ParseHexNumber()
            {
                var t = content[index];
                var next = Peek();
                if (t == '0' && char.ToLower(next) == 'x')
                {
                    var supportedHexNumberChars = "-+0123456789abcdef".ToCharArray();
                    Next(); // skip x
                    var numberValue = "0x";
                    do
                    {
                        t = Next();
                        if (t == '\0' || !supportedHexNumberChars.Contains(char.ToLower(t)))
                            break;
                        numberValue += t;
                    } while (true);

                    Token(TokenType.NumberHex, numberValue);
                }
            }

            void ParseNumber()
            {
                var supportedNumberChars = "-+0123456789.".ToCharArray();
                var t = content[index];
                var numberValue = "";

                var next = Peek();
                if (t == '0' && char.ToLower(next) == 'x')
                {
                    // hexadecimal value
                    ParseHexNumber();
                    return;
                }


                // end of stream: \0
                // 

                while (t != '\0' && supportedNumberChars.Contains(char.ToLower(t)))
                {
                    numberValue += t;

                    if (t == '.')
                    {
                        numberValue = "0" + numberValue;
                    }

                    next = Peek();
                    if (next == '.' || t == '.')
                    {
                        numberValue += Next(); // skip .
                        do
                        {
                            next = Next();
                            if (next == '\0' || !char.IsDigit(next))
                            {
                                Token(TokenType.NumberDecimal, numberValue);
                                return;
                            }

                            numberValue += next;
                        } while (true);
                    }
                    t = Next();
                }
                Token(TokenType.NumberDecimal, numberValue);
            }

            void ParseIdentifier()
            {
                // obviously there's way more invalid chars
                // but easier to list the most common ones that will be used in the parsing
                var invalidIdentifierChars = "-<>|`'¨\"\\/&%#!$§½{}[]()+?*$€£:; \t\r\n\0".ToCharArray();
                var t = content[index];
                var identifier = "";
                while (t != '\0' && !invalidIdentifierChars.Contains(char.ToLower(t)))
                {
                    identifier += t;
                    t = Next();
                }
                Token(TokenType.Identifier, identifier);
            }

            void ParseString(char type, bool allowSingleBackslash = false)
            {
                var stringContent = "";
                // first is ' or ", skip it                
                ++index;

                if (index >= content.Length)
                {
                    return;
                }

                var t = content[index];
                do
                {
                    if (index >= content.Length)
                    {
                        return;
                    }

                    if (t == '\\' && allowSingleBackslash)
                    {
                        stringContent += "\\";
                        t = Next();
                        continue;
                    }

                    if (t == '\\' && Peek() == '\\')
                    {
                        stringContent += $"{t}{Next()}";
                        t = Next();
                        continue;
                    }

                    if (t == '\\' && Peek() == type)
                    {
                        if (allowSingleBackslash)
                        {
                            stringContent += t;
                            break;
                        }

                        stringContent += $"{t}{Next()}";
                        // index += 2;
                        t = Next();
                        continue;
                    }

                    if (t == type)
                    {
                        // empty string
                        break;
                    }

                    stringContent += t;
                    t = Next();
                } while (t != '\0' && t != type);

                Token(type == '\''
                    ? TokenType.SingleQuoteString
                    : TokenType.DoubleQuoteString, stringContent);

                Next();
            }

            if (string.IsNullOrEmpty(content))
                return new TokenStream(tokens);

            while (index < content.Length)
            {
                if (index == lastIndex)
                {
                    //if (logger == null)
                    //{
                    //    throw new StackOverflowException("Stuck in an infinite loop.");
                    //}

                    //logger.WriteWarning("Lexer::Tokenize stuck in infinite loop. Cancelling.");
                    //break;
                }

                lastIndex = index;
                var token = content[index];
                switch (token)
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case ' ':
                        if (keepWhitespace)
                            Token(TokenType.Whitespace);
                        break; // whitespace

                    case '\\':
                        if (keepWhitespace)
                            Token(TokenType.BackSlash);
                        break; // not valid outside string

                    case '"':
                    case '\'':
                        ParseString(token);
                        continue;
                    case '!':
                        {
                            if (Peek() == '=')
                                Token(TokenType.NotEquals, $"{token}{Next()}");
                            else Token(TokenType.Not);
                        }
                        break;
                    case '=':
                        {
                            if (Peek() == token)
                                Token(TokenType.EqualsEquals, $"{token}{Next()}");
                            else
                                Token(TokenType.Equals);
                        }
                        break;
                    case '*':
                        Token(TokenType.Asterisk);
                        break;
                    case '%':
                        Token(TokenType.Percentage);
                        break;
                    case '&':
                        if (Peek() == '&')
                        {
                            Token(TokenType.AndAnd, $"{token}{Next()}");
                            break;
                        }
                        Token(TokenType.And);
                        break;
                    case '|':
                        if (Peek() == '|')
                        {
                            Token(TokenType.OrOr, $"{token}{Next()}");
                            break;
                        }
                        Token(TokenType.Or);
                        break;
                    case '+':
                        if (char.IsDigit(Peek()))
                        {
                            ParseNumber();
                            continue;
                        }
                        if (Peek() == '+')
                        {
                            Token(TokenType.PlusPlus, $"{token}{Next()}");
                            break;
                        }
                        Token(TokenType.Plus);
                        break;
                    case '-':
                        if (char.IsDigit(Peek()))
                        {
                            ParseNumber();
                            continue;
                        }
                        if (Peek() == '-')
                        {
                            Token(TokenType.MinusMinus, $"{token}{Next()}");
                            break;
                        }
                        Token(TokenType.Minus);
                        break;
                    case '<':
                        {
                            if (Peek() == '=')
                                Token(TokenType.LessThanOrEqualsTo, $"{token}{Next()}");
                            else Token(TokenType.LessThan);
                        }
                        break;
                    case '>':
                        {
                            if (Peek() == '=')
                                Token(TokenType.GreaterThanOrEqualsTo, $"{token}{Next()}");
                            else Token(TokenType.GreaterThan);
                        }
                        break;
                    case '{':
                        Token(TokenType.LCurlyBracket);
                        break;
                    case '}':
                        Token(TokenType.RCurlyBracket);
                        break;
                    case '[':
                        Token(TokenType.LBracket);
                        break;
                    case ']':
                        Token(TokenType.RBracket);
                        break;
                    case '(':
                        Token(TokenType.LParen);
                        break;
                    case ')':
                        Token(TokenType.RParen);
                        break;
                    case ';':
                        Token(TokenType.SemiColon);
                        break;
                    case ':':
                        {
                            if (Peek() == token)
                                Token(TokenType.DoubleColon, $"{token}{Next()}");
                            else Token(TokenType.Colon);
                        }
                        break;
                    case '.':
                        if (char.IsNumber(Peek()))
                        {
                            ParseNumber();
                            continue;
                        }
                        Token(TokenType.Dot);
                        break;
                    case ',':
                        Token(TokenType.Comma);
                        break;
                    case '?':
                        Token(TokenType.QuestionMark);
                        break;
                    case '$':
                        Token(TokenType.DollarSign);
                        break;
                    case '@':
                        var nc = Peek();
                        if (nc == '"' || (nc == '$' && Peek(2) == '"'))
                        {
                            ParseString(Next(), true);
                            continue;
                        }
                        Token(TokenType.At, "@");
                        break;
                    case '#':
                        Token(TokenType.HashTag);
                        break;
                    case '/':
                        {
                            var next = Peek();
                            if (next == '*')
                            {
                                ParseMultilineComment();
                                continue;
                            }
                            if (next == token)
                            {
                                ParseSinglelineComment();
                                continue;
                            }

                            Token(TokenType.Slash);
                        }
                        break;
                    //case '0':
                    //case '1':
                    //case '2':
                    //case '3':
                    //case '4':
                    //case '5':
                    //case '6':
                    //case '7':
                    //case '8':
                    //case '9':
                    //    ParseNumber();
                    //    continue;
                    default:
                        ParseIdentifier();
                        continue;
                }

                ++index;
            }

            return new TokenStream(tokens);
        }
    }
}