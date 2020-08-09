using MarineLang.Models;
using MarineLang.Streams;
using System;
using System.Collections.Generic;

namespace MarineLang.LexicalAnalysis
{
    public class Lexer
    {
        public IEnumerable<Token> GetTokens(string str)
        {
            var stream = IndexedCharStream.Create(str);
            stream.MoveNext();

            if (stream.IsEnd == false)
                LexerHelper.ManySkip(stream);
            while (stream.IsEnd == false)
            {
                yield return
                    LexerHelper.GetStringToken("&&", TokenType.Op)(stream) ??
                    LexerHelper.GetStringToken("||", TokenType.Op)(stream) ??
                    LexerHelper.GetStringToken("==", TokenType.Op)(stream) ??
                    LexerHelper.GetStringToken("!=", TokenType.Op)(stream) ??
                    LexerHelper.GetStringTokenTailSkip("fun", TokenType.Func, stream) ??
                    LexerHelper.GetStringTokenTailSkip("end", TokenType.End, stream) ??
                    LexerHelper.GetStringTokenTailSkip("true", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailSkip("false", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailSkip("ret", TokenType.Return, stream) ??
                    LexerHelper.GetIntLiteralToken()(stream) ??
                    LexerHelper.GetCharToken(stream, '(', TokenType.LeftParen) ??
                    LexerHelper.GetCharToken(stream, ')', TokenType.RightParen) ??
                    LexerHelper.GetCharToken(stream, '.', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '+', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '-', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '*', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '/', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '%', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '&', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '|', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '=', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, ',', TokenType.Comma) ??
                    LexerHelper.GetIdToken()(stream) ??
                    LexerHelper.GetUnknownToken(stream);

                if (stream.IsEnd == false)
                    LexerHelper.ManySkip(stream);
            }
        }
    }
}
