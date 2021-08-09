using MarineLang.Models;
using MarineLang.Inputs;
using System;

namespace MarineLang.LexicalAnalysis
{
    public static class LexerHelper
    {
        static public bool Delimiter(IndexedCharInput input)
        {
            if (Skip(input))
                return true;
            if (char.IsLetterOrDigit(input.Current.c) == false && input.Current.c != '_')
                return true;
            return false;
        }
        static public bool Skip(IndexedCharInput input)
        {
            if (CommentOut(input))
            {
                return true;
            }

            if (
                input.Current.c == ' ' ||
                input.Current.c == '\n' ||
                input.Current.c == '\r' ||
                input.Current.c == '\t'
                )
            {
                input.MoveNext();
                return true;
            }
            return false;
        }

        static public bool CommentOut(IndexedCharInput input)
        {
            var backUpIndex = input.Index;
            if (input.Current.c == '/' && input.MoveNext())
            {
                if (input.Current.c == '/')
                {
                    while (input.MoveNext() && input.Current.c != '\n') ;
                    input.MoveNext();
                    return true;
                }
                else if (input.Current.c == '*')
                {
                    while (input.MoveNext())
                        if (input.Current.c == '*' && input.MoveNext() && input.Current.c == '/')
                            break;
                    input.MoveNext();
                    return true;
                }
            }

            input.SetIndex(backUpIndex);
            return false;
        }

        static public bool ManySkip(IndexedCharInput input)
        {
            if (Skip(input) == false)
                return false;

            while (input.IsEnd == false && Skip(input)) ;
            return true;
        }

        static public Token GetCharToken(IndexedCharInput input, TokenType tokenType)
        {
            return GetCharToken(input, tokenType.GetText()[0], tokenType);
        }

            static public Token GetCharToken(IndexedCharInput input, char c, TokenType tokenType)
        {
            var indexedChar = input.Current;
            if (indexedChar.c == c)
            {
                input.MoveNext();
                return new Token(tokenType, indexedChar.c.ToString(), indexedChar.position);
            }
            return null;
        }

        static public Func<IndexedCharInput, Token> GetStringToken(TokenType tokenType)
        {
            return GetStringToken(tokenType.GetText(), tokenType);
        }

        static public Func<IndexedCharInput, Token> GetStringToken(string str, TokenType tokenType)
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

        static public Token GetStringTokenTailDelimiter(TokenType tokenType, IndexedCharInput input)
        {
            return GetStringTokenTailDelimiter(tokenType.GetText(), tokenType, input);
        }

        static public Token GetStringTokenTailDelimiter(string str, TokenType tokenType, IndexedCharInput input)
        {
            var backUpIndex = input.Index;
            var token = GetStringToken(str, tokenType)(input);
            if (token == null)
                return null;
            if (input.IsEnd == true || Delimiter(input))
                return token;

            input.SetIndex(backUpIndex);
            return null;
        }

        static public Func<IndexedCharInput, Token> GetIdToken()
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

        static public Func<IndexedCharInput, Token> GetClassNameToken()
        {
            return
             GetTokenTest(TokenType.ClassName, (count, c) =>
             {
                 if (count == 0 && IsUpperLetter(c) == false)
                     return TestResult.End;
                 if ((char.IsLetter(c) || char.IsDigit(c)) == false)
                     return TestResult.End;
                 return TestResult.Pass;
             }
             );
        }

        static public Func<IndexedCharInput, Token> GetMacroNameToken()
        {
            return
             GetTokenTest(TokenType.MacroName, (count, c) =>
             {
                 if (count == 0)
                     return c == '#' ? TestResult.Pass : TestResult.End;
                 if ((char.IsLetter(c) || char.IsDigit(c)) == false)
                     return TestResult.End;
                 return TestResult.Pass;
             }
             );
        }

        static public Func<IndexedCharInput, Token> GetIntLiteralToken()
        {
            return
                GetTokenTest(TokenType.Int, (_, c) =>
                   char.IsDigit(c) ? TestResult.Pass : TestResult.End
                );
        }

        static public Token GetCharLiteralToken(IndexedCharInput input)
        {
            var indexedChar = input.Current;
            var backUpIndex = input.Index;

            if (indexedChar.c != '\'')
                return null;
            if (input.MoveNext() == false)
            {
                input.SetIndex(backUpIndex);
                return null;
            }

            var value = input.Current.c;

            if (value == '\'')
            {
                input.SetIndex(backUpIndex);
                return null;
            }

            if (value == '\\')
            {
                var escapeChar = GetEscapeChar(input);
                if (escapeChar.HasValue == false)
                {
                    input.SetIndex(backUpIndex);
                    return null;
                }
                value = escapeChar.Value;
            }
            else
                input.MoveNext();

            if (input.IsEnd || input.Current.c != '\'')
            {
                input.SetIndex(backUpIndex);
                return null;
            }

            input.MoveNext();

            return new Token(TokenType.Char, "'" + value + "'", indexedChar.position);
        }

        static public Token GetStringLiteralToken(IndexedCharInput input)
        {
            var indexedChar = input.Current;
            var backUpIndex = input.Index;

            if (indexedChar.c != '"')
                return null;

            var value = "";
            input.MoveNext();

            while (input.IsEnd == false && input.Current.c != '"')
            {
                var escapeChar = GetEscapeChar(input);
                if (escapeChar.HasValue)
                    value += escapeChar.Value;
                else
                {
                    value += input.Current.c;
                    input.MoveNext();
                }
            }

            if (input.IsEnd)
            {
                input.SetIndex(backUpIndex);
                return null;
            }

            input.MoveNext();

            return new Token(TokenType.String, "\"" + value + "\"", indexedChar.position);
        }

        static public char? GetEscapeChar(IndexedCharInput input)
        {
            var value = input.Current.c;
            if (value != '\\')
                return null;
            if (input.MoveNext() == false)
            {
                input.SetIndex(input.Index - 1);
                return null;
            }
            var escapeChar = ToEspaceChar(value.ToString() + input.Current.c);
            if (escapeChar.HasValue == false)
            {
                input.SetIndex(input.Index - 1);
                return null;
            }
            input.MoveNext();
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

        static public Token GetFloatLiteralToken(IndexedCharInput input)
        {
            var indexedChar = input.Current;
            var backUpIndex = input.Index;
            var buf = "";

            var head = GetIntLiteralToken()(input)?.text;
            if (head == null)
                return null;

            buf += head;

            if (
                input.IsEnd ||
                input.Current.c != '.' ||
                input.MoveNext() == false
            )
            {
                input.SetIndex(backUpIndex);
                return null;
            }
            buf += '.';

            var tail = GetIntLiteralToken()(input)?.text;
            if (tail == null)
            {
                input.SetIndex(backUpIndex);
                return null;
            }
            buf += tail;

            return new Token(TokenType.Float, buf, indexedChar.position);

        }

        static public Token GetUnknownToken(IndexedCharInput input)
        {
            var indexedChar = input.Current;
            var buf = input.Current.c.ToString();

            while (input.MoveNext())
            {
                if (ManySkip(input))
                    break;
                buf += input.Current.c;
            }
            return new Token(TokenType.UnKnown, buf, indexedChar.position);

        }

        static Func<IndexedCharInput, Token> GetTokenTest(TokenType tokenType, Func<int, char, TestResult> test)
        {

            return input =>
            {
                var backUpIndex = input.Index;
                var firstIndexedChar = input.Current;
                var buf = "";
                var isContinue = false;

                while (input.IsEnd == false)
                {
                    var indexedChar = input.Current;
                    var testResult = test(input.Index - backUpIndex, indexedChar.c);
                    if (testResult == TestResult.End)
                        break;

                    isContinue = testResult == TestResult.Continue;

                    buf += indexedChar.c;
                    input.MoveNext();
                }

                if (string.IsNullOrEmpty(buf))
                    return null;

                if (isContinue)
                {
                    input.SetIndex(backUpIndex);
                    return null;
                }

                return new Token(tokenType, buf, firstIndexedChar.position);
            };
        }

        static bool IsLowerLetter(char c)
        {
            return char.IsLetter(c) && char.IsLower(c);
        }

        static bool IsUpperLetter(char c)
        {
            return char.IsLetter(c) && char.IsUpper(c);
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
