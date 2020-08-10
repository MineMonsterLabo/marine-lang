using MarineLang.Models;
using MarineLang.Streams;
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
                    LexerHelper.GetStringTokenTailDelimiter("fun", TokenType.Func, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("end", TokenType.End, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("let", TokenType.Let, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("true", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("false", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("ret", TokenType.Return, stream) ??
                    LexerHelper.GetFloatLiteralToken(stream) ??
                    LexerHelper.GetIntLiteralToken()(stream) ??
                    LexerHelper.GetCharLiteralToken(stream) ??
                    LexerHelper.GetStringLiteralToken(stream) ??
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
                    LexerHelper.GetCharToken(stream, '=', TokenType.AssignmentOp) ??
                    LexerHelper.GetCharToken(stream, ',', TokenType.Comma) ??
                    LexerHelper.GetIdToken()(stream) ??
                    LexerHelper.GetUnknownToken(stream);

                if (stream.IsEnd == false)
                    LexerHelper.ManySkip(stream);
            }
        }
    }
}
