using MarineLang.Models;
using MarineLang.Inputs;
using System.Collections.Generic;

namespace MarineLang.LexicalAnalysis
{
    public class LexicalAnalyzer
    {
        public IEnumerable<Token> GetTokens(string str)
        {
            var input = IndexedCharInput.Create(str);

            if (input.IsEnd == false)
                LexerHelper.ManySkip(input);
            while (input.IsEnd == false)
            {
                yield return
                    LexerHelper.GetStringToken(TokenType.AndOp)(input) ??
                    LexerHelper.GetStringToken(TokenType.OrOp)(input) ??
                    LexerHelper.GetStringToken(TokenType.EqualOp)(input) ??
                    LexerHelper.GetStringToken(TokenType.GreaterEqualOp)(input) ??
                    LexerHelper.GetStringToken(TokenType.LessEqualOp)(input) ??
                    LexerHelper.GetStringToken(TokenType.NotEqualOp)(input) ??
                    LexerHelper.GetStringToken(TokenType.TwoColon)(input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Await, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Yield, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Break, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.If, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Else, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Func, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.End, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Let, input) ??
                    LexerHelper.GetStringTokenTailDelimiter("true", TokenType.Bool, input) ??
                    LexerHelper.GetStringTokenTailDelimiter("false", TokenType.Bool, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.Return, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.While, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.For, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.ForEach, input) ??
                    LexerHelper.GetStringTokenTailDelimiter(TokenType.In, input) ??
                    LexerHelper.GetFloatLiteralToken(input) ??
                    LexerHelper.GetIntLiteralToken()(input) ??
                    LexerHelper.GetCharLiteralToken(input) ??
                    LexerHelper.GetStringLiteralToken(input) ??
                    LexerHelper.GetCharToken(input, TokenType.Semicolon) ??
                    LexerHelper.GetCharToken(input, TokenType.LeftCurlyBracket) ??
                    LexerHelper.GetCharToken(input, TokenType.RightCurlyBracket) ??
                    LexerHelper.GetCharToken(input, TokenType.LeftBracket) ??
                    LexerHelper.GetCharToken(input, TokenType.RightBracket) ??
                    LexerHelper.GetCharToken(input, TokenType.LeftParen) ??
                    LexerHelper.GetCharToken(input, TokenType.RightParen) ??
                    LexerHelper.GetCharToken(input, TokenType.DotOp) ??
                    LexerHelper.GetCharToken(input, TokenType.GreaterOp) ??
                    LexerHelper.GetCharToken(input, TokenType.LessOp) ??
                    LexerHelper.GetCharToken(input, TokenType.PlusOp) ??
                    LexerHelper.GetCharToken(input, TokenType.MinusOp) ??
                    LexerHelper.GetCharToken(input, TokenType.MulOp) ??
                    LexerHelper.GetCharToken(input, TokenType.DivOp) ??
                    LexerHelper.GetCharToken(input, TokenType.ModOp) ??
                    LexerHelper.GetCharToken(input, TokenType.PipeOp) ??
                    LexerHelper.GetCharToken(input, TokenType.AssignmentOp) ??
                    LexerHelper.GetCharToken(input, TokenType.Comma) ??
                    LexerHelper.GetCharToken(input, TokenType.NotOp) ??
                    LexerHelper.GetIdToken()(input) ??
                    LexerHelper.GetClassNameToken()(input) ??
                    LexerHelper.GetMacroNameToken()(input) ??
                    LexerHelper.GetUnknownToken(input);

                if (input.IsEnd == false)
                    LexerHelper.ManySkip(input);
            }
        }
    }
}
