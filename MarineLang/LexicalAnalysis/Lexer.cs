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
                    LexerHelper.GetStringToken("&&", TokenType.AndOp)(stream) ??
                    LexerHelper.GetStringToken("||", TokenType.OrOp)(stream) ??
                    LexerHelper.GetStringToken("==", TokenType.EqualOp)(stream) ??
                    LexerHelper.GetStringToken(">=", TokenType.GreaterEqualOp)(stream) ??
                    LexerHelper.GetStringToken("<=", TokenType.LessEqualOp)(stream) ??
                    LexerHelper.GetStringToken("!=", TokenType.NotEqualOp)(stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("await", TokenType.Await, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("yield", TokenType.Yield, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("if", TokenType.If, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("else", TokenType.Else, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("fun", TokenType.Func, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("end", TokenType.End, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("let", TokenType.Let, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("true", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("false", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("ret", TokenType.Return, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("while", TokenType.While, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("for", TokenType.For, stream) ??
                    LexerHelper.GetFloatLiteralToken(stream) ??
                    LexerHelper.GetIntLiteralToken()(stream) ??
                    LexerHelper.GetCharLiteralToken(stream) ??
                    LexerHelper.GetStringLiteralToken(stream) ??
                    LexerHelper.GetCharToken(stream, ';', TokenType.Semicolon) ??
                    LexerHelper.GetCharToken(stream, '{', TokenType.LeftCurlyBracket) ??
                    LexerHelper.GetCharToken(stream, '}', TokenType.RightCurlyBracket) ??
                    LexerHelper.GetCharToken(stream, '[', TokenType.LeftBracket) ??
                    LexerHelper.GetCharToken(stream, ']', TokenType.RightBracket) ??
                    LexerHelper.GetCharToken(stream, '(', TokenType.LeftParen) ??
                    LexerHelper.GetCharToken(stream, ')', TokenType.RightParen) ??
                    LexerHelper.GetCharToken(stream, '.', TokenType.DotOp) ??
                    LexerHelper.GetCharToken(stream, '>', TokenType.GreaterOp) ??
                    LexerHelper.GetCharToken(stream, '<', TokenType.LessOp) ??
                    LexerHelper.GetCharToken(stream, '+', TokenType.PlusOp) ??
                    LexerHelper.GetCharToken(stream, '-', TokenType.MinusOp) ??
                    LexerHelper.GetCharToken(stream, '*', TokenType.MulOp) ??
                    LexerHelper.GetCharToken(stream, '/', TokenType.DivOp) ??
                    LexerHelper.GetCharToken(stream, '%', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '&', TokenType.Op) ??
                    LexerHelper.GetCharToken(stream, '|', TokenType.PipeOp) ??
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
