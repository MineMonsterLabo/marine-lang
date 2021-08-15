using MarineLang.Models;
using MarineLang.Inputs;
using System.Collections.Generic;
using MarineLang.ParserCore;
using MineUtil;

namespace MarineLang.LexicalAnalysis
{
    public class LexicalAnalyzer
    {
        public IEnumerable<Token> GetTokens(string str)
        {
            IInput<char> input = CharInput.Create(str);
            return 
                LexerHelper.ManySkip()
                .Right(
                    Parse<char>.ManyUntilEnd(GetToken().Left(LexerHelper.ManySkip()))
                )
                (input)
                .Result.Unwrap();
        }

        public Parse<char>.Parser<Token> GetToken()
        {
            return Parse<char>.Or(
                LexerHelper.GetStringToken(TokenType.AndOp),
                LexerHelper.GetStringToken(TokenType.OrOp),
                LexerHelper.GetStringToken(TokenType.EqualOp),
                LexerHelper.GetStringToken(TokenType.GreaterEqualOp),
                LexerHelper.GetStringToken(TokenType.LessEqualOp),
                LexerHelper.GetStringToken(TokenType.NotEqualOp),
                LexerHelper.GetStringToken(TokenType.TwoColon),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Await),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Yield),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Break),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.If),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Else),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Func),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.End),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Let),
                LexerHelper.GetStringTokenTailDelimiter("true", TokenType.Bool),
                LexerHelper.GetStringTokenTailDelimiter("false", TokenType.Bool),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.Return),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.While),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.For),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.ForEach),
                LexerHelper.GetStringTokenTailDelimiter(TokenType.In),
                LexerHelper.GetFloatLiteralToken().Try(),
                LexerHelper.GetIntLiteralToken(),
                LexerHelper.GetCharLiteralToken(),
                LexerHelper.GetStringLiteralToken(),
                LexerHelper.GetCharToken(TokenType.Semicolon),
                LexerHelper.GetCharToken(TokenType.LeftCurlyBracket),
                LexerHelper.GetCharToken(TokenType.RightCurlyBracket),
                LexerHelper.GetCharToken(TokenType.LeftBracket),
                LexerHelper.GetCharToken(TokenType.RightBracket),
                LexerHelper.GetCharToken(TokenType.LeftParen),
                LexerHelper.GetCharToken(TokenType.RightParen),
                LexerHelper.GetCharToken(TokenType.DotOp),
                LexerHelper.GetCharToken(TokenType.GreaterOp),
                LexerHelper.GetCharToken(TokenType.LessOp),
                LexerHelper.GetCharToken(TokenType.PlusOp),
                LexerHelper.GetCharToken(TokenType.MinusOp),
                LexerHelper.GetCharToken(TokenType.MulOp),
                LexerHelper.GetCharToken(TokenType.DivOp),
                LexerHelper.GetCharToken(TokenType.ModOp),
                LexerHelper.GetCharToken(TokenType.PipeOp),
                LexerHelper.GetCharToken(TokenType.AssignmentOp),
                LexerHelper.GetCharToken(TokenType.Comma),
                LexerHelper.GetCharToken(TokenType.NotOp),
                LexerHelper.GetIdToken(),
                LexerHelper.GetClassNameToken(),
                LexerHelper.GetMacroNameToken(),
                LexerHelper.GetUnknownToken()
            );
        }
    }
}
