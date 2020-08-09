using MarineLang.Models;
using MarineLang.Streams;
using System;

namespace MarineLang.LexicalAnalysis
{
    public static class LexerHelper
    {
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
                return new Token(tokenType, indexedChar.c.ToString(), indexedChar.index, indexedChar.index);
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

        static public Token GetStringTokenTailSkip(string str, TokenType tokenType, IndexedCharStream stream)
        {
            var backUpIndex = stream.Index;
            var token = GetStringToken(str, tokenType)(stream);
            if (token == null)
                return null;
            if (stream.IsEnd == true || Skip(stream))
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
            var begin = indexedChar.index;

            if (indexedChar.c != '\'')
                return null;
            if (stream.MoveNext() == false)
            {
                stream.SetIndex(begin);
                return null;
            }

            var value = stream.Current.c;

            if (value == '\'')
            {
                stream.SetIndex(begin);
                return null;
            }

            if (value == '\\')
            {
                if (stream.MoveNext() == false)
                {
                    stream.SetIndex(begin);
                    return null;
                }
                var value2 = ToEspaceChar(value.ToString() + stream.Current.c);
                if (value2.HasValue == false)
                {
                    stream.SetIndex(begin);
                    return null;
                }
                value = value2.Value;
            }

            if (stream.MoveNext() == false || stream.Current.c != '\'')
            {
                stream.SetIndex(begin);
                return null;
            }

            stream.MoveNext();

            return new Token(TokenType.Char, "'" + value + "'", begin, stream.Index - 1);
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
            }
            return null;
        }

        static public Token GetUnknownToken(IndexedCharStream stream)
        {
            var begin = stream.Current.index;
            var end = begin;
            var buf = stream.Current.c.ToString();

            while (stream.MoveNext())
            {
                if (ManySkip(stream))
                    break;
                end = stream.Current.index;
                buf += stream.Current.c;
            }
            return new Token(TokenType.UnKnown, buf, begin, end);

        }

        static Func<IndexedCharStream, Token> GetTokenTest(TokenType tokenType, Func<int, char, TestResult> test)
        {

            return stream =>
            {
                var backUpIndex = stream.Index;
                var begin = stream.Current.index;
                var end = begin;
                var buf = "";
                var isContinue = false;

                while (stream.IsEnd == false)
                {
                    var indexedChar = stream.Current;
                    var testResult = test(indexedChar.index - backUpIndex, indexedChar.c);
                    if (testResult == TestResult.End)
                        break;

                    isContinue = testResult == TestResult.Continue;

                    end = indexedChar.index;
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

                return new Token(tokenType, buf, begin, end);
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
