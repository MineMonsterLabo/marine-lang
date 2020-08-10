using MarineLang.Models;
using MarineLang.Streams;
using System;

namespace MarineLang.LexicalAnalysis
{
    public static class LexerHelper
    {
        static public bool Delimiter(IndexedCharStream stream)
        {
            if (Skip(stream))
                return true;
            if (char.IsLetterOrDigit(stream.Current.c) == false && stream.Current.c != '_')
                return true;
            return false;
        }
        static public bool Skip(IndexedCharStream stream)
        {
            if (
                stream.Current.c == ' ' ||
                stream.Current.c == '\n' ||
                stream.Current.c == '\r' ||
                stream.Current.c == '\t'
                )
            {
                stream.MoveNext();
                return true;
            }
            return false;
        }

        static public bool ManySkip(IndexedCharStream stream)
        {
            if (Skip(stream) == false)
                return false;

            while (stream.IsEnd == false && Skip(stream)) ;
            return true;
        }

        static public Token GetCharToken(IndexedCharStream stream, char c, TokenType tokenType)
        {
            var indexedChar = stream.Current;
            if (indexedChar.c == c)
            {
                stream.MoveNext();
                return new Token(tokenType, indexedChar.c.ToString(), indexedChar.position);
            }
            return null;
        }

        static public Func<IndexedCharStream, Token> GetStringToken(string str, TokenType tokenType)
        {
            return
             GetTokenTest(tokenType, (count, c) =>
             {
                 if (str.Length <= count)
                     return TestResult.End;
                 if (c == str[count])
                     return str.Length - 1 == count ? TestResult.Pass : TestResult.Continue;
                 return TestResult.End;
             }
             );
        }

        static public Token GetStringTokenTailDelimiter(string str, TokenType tokenType, IndexedCharStream stream)
        {
            var backUpIndex = stream.Index;
            var token = GetStringToken(str, tokenType)(stream);
            if (token == null)
                return null;
            if (stream.IsEnd == true || Delimiter(stream))
                return token;

            stream.SetIndex(backUpIndex);
            return null;
        }

        static public Func<IndexedCharStream, Token> GetIdToken()
        {
            return
             GetTokenTest(TokenType.Id, (count, c) =>
             {
                 if (count == 0 && IsLowerLetter(c) == false)
                     return TestResult.End;
                 if (IsIdChar(c) == false)
                     return TestResult.End;
                 return TestResult.Pass;
             }
             );
        }

        static public Func<IndexedCharStream, Token> GetIntLiteralToken()
        {
            return
                GetTokenTest(TokenType.Int, (_, c) =>
                   char.IsDigit(c) ? TestResult.Pass : TestResult.End
                );
        }

        static public Token GetCharLiteralToken(IndexedCharStream stream)
        {
            var indexedChar = stream.Current;
            var backUpIndex = stream.Index;

            if (indexedChar.c != '\'')
                return null;
            if (stream.MoveNext() == false)
            {
                stream.SetIndex(backUpIndex);
                return null;
            }

            var value = stream.Current.c;

            if (value == '\'')
            {
                stream.SetIndex(backUpIndex);
                return null;
            }

            if (value == '\\')
            {
                var escapeChar = GetEscapeChar(stream);
                if (escapeChar.HasValue == false)
                {
                    stream.SetIndex(backUpIndex);
                    return null;
                }
                value = escapeChar.Value;
            }
            else
                stream.MoveNext();

            if (stream.IsEnd || stream.Current.c != '\'')
            {
                stream.SetIndex(backUpIndex);
                return null;
            }

            stream.MoveNext();

            return new Token(TokenType.Char, "'" + value + "'", indexedChar.position);
        }

        static public Token GetStringLiteralToken(IndexedCharStream stream)
        {
            var indexedChar = stream.Current;
            var backUpIndex = stream.Index;

            if (indexedChar.c != '"')
                return null;

            var value = "";
            stream.MoveNext();

            while (stream.IsEnd == false && stream.Current.c != '"')
            {
                var escapeChar = GetEscapeChar(stream);
                if (escapeChar.HasValue)
                    value += escapeChar.Value;
                else
                {
                    value += stream.Current.c;
                    stream.MoveNext();
                }
            }

            if (stream.IsEnd)
            {
                stream.SetIndex(backUpIndex);
                return null;
            }

            stream.MoveNext();

            return new Token(TokenType.String, "\"" + value + "\"", indexedChar.position);
        }

        static public char? GetEscapeChar(IndexedCharStream stream)
        {
            var value = stream.Current.c;
            if (value != '\\')
                return null;
            if (stream.MoveNext() == false)
            {
                stream.SetIndex(stream.Index - 1);
                return null;
            }
            var escapeChar = ToEspaceChar(value.ToString() + stream.Current.c);
            if (escapeChar.HasValue == false)
            {
                stream.SetIndex(stream.Index - 1);
                return null;
            }
            stream.MoveNext();
            return escapeChar.Value;
        }

        static public char? ToEspaceChar(string str)
        {
            switch (str)
            {
                case "\\\\":
                    return '\\';
                case "\\r":
                    return '\r';
                case "\\n":
                    return '\n';
                case "\\t":
                    return '\t';
                case "\\'":
                    return '\'';
                case "\\\"":
                    return '\"';
            }
            return null;
        }

        static public Token GetFloatLiteralToken(IndexedCharStream stream)
        {
            var indexedChar = stream.Current;
            var backUpIndex = stream.Index;
            var buf = "";

            var head = GetIntLiteralToken()(stream)?.text;
            if (head == null)
                return null;

            buf += head;

            if (
                stream.IsEnd ||
                stream.Current.c != '.' ||
                stream.MoveNext() == false
            )
            {
                stream.SetIndex(backUpIndex);
                return null;
            }
            buf += '.';

            var tail = GetIntLiteralToken()(stream)?.text;
            if (tail == null)
            {
                stream.SetIndex(backUpIndex);
                return null;
            }
            buf += tail;

            return new Token(TokenType.Float, buf, indexedChar.position);

        }

        static public Token GetUnknownToken(IndexedCharStream stream)
        {
            var indexedChar = stream.Current;
            var buf = stream.Current.c.ToString();

            while (stream.MoveNext())
            {
                if (ManySkip(stream))
                    break;
                buf += stream.Current.c;
            }
            return new Token(TokenType.UnKnown, buf, indexedChar.position);

        }

        static Func<IndexedCharStream, Token> GetTokenTest(TokenType tokenType, Func<int, char, TestResult> test)
        {

            return stream =>
            {
                var backUpIndex = stream.Index;
                var firstIndexedChar = stream.Current;
                var buf = "";
                var isContinue = false;

                while (stream.IsEnd == false)
                {
                    var indexedChar = stream.Current;
                    var testResult = test(stream.Index - backUpIndex, indexedChar.c);
                    if (testResult == TestResult.End)
                        break;

                    isContinue = testResult == TestResult.Continue;

                    buf += indexedChar.c;
                    stream.MoveNext();
                }

                if (string.IsNullOrEmpty(buf))
                    return null;

                if (isContinue)
                {
                    stream.SetIndex(backUpIndex);
                    return null;
                }

                return new Token(tokenType, buf, firstIndexedChar.position);
            };
        }

        static bool IsLowerLetter(char c)
        {
            return char.IsLetter(c) && char.IsLower(c);
        }

        static bool IsIdChar(char c)
        {
            return char.IsDigit(c) || IsLowerLetter(c) || c == '_';
        }

        enum TestResult
        {
            Pass,
            End,
            Continue
        }
    }
}
