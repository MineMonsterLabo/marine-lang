using System;
using System.Collections.Generic;

namespace MarineLang
{
    public class Lexer
    {
        public IEnumerable<Token> GetTokens(string str)
        {
            var stream = IndexedCharStream.Create(str);
            stream.MoveNext();

            if (stream.IsEnd == false)
                ManySkip(stream);
            while (stream.IsEnd == false)
            {
                yield return
                    GetStringToken("&&", TokenType.Op)(stream) ??
                    GetStringToken("||", TokenType.Op)(stream) ??
                    GetStringToken("==", TokenType.Op)(stream) ??
                    GetStringToken("!=", TokenType.Op)(stream) ??
                    GetStringTokenTailSkip("fun", TokenType.Func, stream) ??
                    GetStringTokenTailSkip("end", TokenType.End, stream) ??
                    GetStringTokenTailSkip("true", TokenType.Bool, stream) ??
                    GetStringTokenTailSkip("false", TokenType.Bool, stream) ??
                    GetIntToken()(stream) ??
                    GetCharToken(stream, '(', TokenType.LeftParen) ??
                    GetCharToken(stream, ')', TokenType.RightParen) ??
                    GetCharToken(stream, '.', TokenType.Op) ??
                    GetCharToken(stream, '+', TokenType.Op) ??
                    GetCharToken(stream, '-', TokenType.Op) ??
                    GetCharToken(stream, '*', TokenType.Op) ??
                    GetCharToken(stream, '/', TokenType.Op) ??
                    GetCharToken(stream, '%', TokenType.Op) ??
                    GetCharToken(stream, '&', TokenType.Op) ??
                    GetCharToken(stream, '|', TokenType.Op) ??
                    GetCharToken(stream, '=', TokenType.Op) ??
                    GetCharToken(stream, ',', TokenType.Comma) ??
                    GetIdToken()(stream) ??
                    GetUnknownToken(stream);

                if (stream.IsEnd == false)
                    ManySkip(stream);
            }

        }

        //fun hoge_fuga(monster,event_npc) 4+7-4/5*2%(3!=4&&5==3||1)

        bool Skip(IndexedCharStream stream)
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

        bool ManySkip(IndexedCharStream stream)
        {
            if (Skip(stream) == false)
            {
                return false;
            }
            while (stream.IsEnd == false && Skip(stream)) ;
            return true;
        }

        Token GetCharToken(IndexedCharStream stream, char c, TokenType tokenType)
        {
            var indexedChar = stream.Current;
            if (indexedChar.c == c)
            {
                stream.MoveNext();
                return new Token(tokenType, indexedChar.c.ToString(), indexedChar.index, indexedChar.index);
            }
            return null;
        }

        Func<IndexedCharStream, Token> GetStringToken(string str, TokenType tokenType)
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

        Token GetStringTokenTailSkip(string str, TokenType tokenType, IndexedCharStream stream)
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

        Func<IndexedCharStream, Token> GetIdToken()
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

        Func<IndexedCharStream, Token> GetIntToken()
        {
            return
                GetTokenTest(TokenType.Int, (_, c) =>
                   char.IsDigit(c) ? TestResult.Pass : TestResult.End
                );
        }

        Func<IndexedCharStream, Token> GetTokenTest(TokenType tokenType, Func<int, char, TestResult> test)
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

        bool IsLowerLetter(char c)
        {
            return char.IsLetter(c) && char.IsLower(c);
        }

        bool IsIdChar(char c)
        {
            return char.IsDigit(c) || IsLowerLetter(c) || c == '_';
        }


        Token GetUnknownToken(IndexedCharStream stream)
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

        public enum TestResult
        {
            Pass,
            End,
            Continue
        }
    }
}
