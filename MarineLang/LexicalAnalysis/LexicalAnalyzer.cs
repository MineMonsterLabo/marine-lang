﻿using MarineLang.Models;
using MarineLang.Streams;
using System.Collections.Generic;

namespace MarineLang.LexicalAnalysis
{
    public class LexicalAnalyzer
    {
        public IEnumerable<Token> GetTokens(string str)
        {
            var stream = IndexedCharStream.Create(str);

            if (stream.IsEnd == false)
                LexerHelper.ManySkip(stream);
            while (stream.IsEnd == false)
            {
                yield return
                    LexerHelper.GetStringToken(TokenType.AndOp)(stream) ??
                    LexerHelper.GetStringToken(TokenType.OrOp)(stream) ??
                    LexerHelper.GetStringToken(TokenType.EqualOp)(stream) ??
                    LexerHelper.GetStringToken(TokenType.GreaterEqualOp)(stream) ??
                    LexerHelper.GetStringToken(TokenType.LessEqualOp)(stream) ??
                    LexerHelper.GetStringToken(TokenType.NotEqualOp)(stream) ??
                    LexerHelper.GetStringToken(TokenType.TwoColon)(stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Await, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Yield, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Break, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.If, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Else, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Func, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.End, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Let, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("true", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter("false", TokenType.Bool, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Return, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.While, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.For, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.ForEach, stream) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.In, stream) ??
                    LexerHelper.GetFloatLiteralToken(stream) ??
                    LexerHelper.GetIntLiteralToken()(stream) ??
                    LexerHelper.GetCharLiteralToken(stream) ??
                    LexerHelper.GetStringLiteralToken(stream) ??
                    LexerHelper.GetCharToken(stream, TokenType.Semicolon) ??
                    LexerHelper.GetCharToken(stream, TokenType.LeftCurlyBracket) ??
                    LexerHelper.GetCharToken(stream, TokenType.RightCurlyBracket) ??
                    LexerHelper.GetCharToken(stream, TokenType.LeftBracket) ??
                    LexerHelper.GetCharToken(stream, TokenType.RightBracket) ??
                    LexerHelper.GetCharToken(stream, TokenType.LeftParen) ??
                    LexerHelper.GetCharToken(stream, TokenType.RightParen) ??
                    LexerHelper.GetCharToken(stream, TokenType.DotOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.GreaterOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.LessOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.PlusOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.MinusOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.MulOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.DivOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.ModOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.PipeOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.AssignmentOp) ??
                    LexerHelper.GetCharToken(stream, TokenType.Comma) ??
                    LexerHelper.GetCharToken(stream, TokenType.NotOp) ??
                    LexerHelper.GetIdToken()(stream) ??
                    LexerHelper.GetClassNameToken()(stream) ??
                    LexerHelper.GetMacroNameToken()(stream) ??
                    LexerHelper.GetUnknownToken(stream);

                if (stream.IsEnd == false)
                    LexerHelper.ManySkip(stream);
            }
        }
    }
}
