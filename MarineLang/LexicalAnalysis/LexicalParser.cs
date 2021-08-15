using MarineLang.Models;
using MarineLang.ParserCore;
using MineUtil;
using System.Collections.Generic;

namespace MarineLang.LexicalAnalysis
{
    using Parse = Parse<char>;

    public static class LexicalParser
    {
        static public Parse.Parser<IEnumerable<Token>> Main()
        {
            return
                ManySkip()
                .Right(
                    Parse.ManyUntilEnd(GetToken().Left(ManySkip()))
                );
        }

        static public Parse.Parser<Token> GetToken()
        {
            return Parse.Or(
                GetStringToken(TokenType.AndOp),
                GetStringToken(TokenType.OrOp),
                GetStringToken(TokenType.EqualOp),
                GetStringToken(TokenType.GreaterEqualOp),
                GetStringToken(TokenType.LessEqualOp),
                GetStringToken(TokenType.NotEqualOp),
                GetStringToken(TokenType.TwoColon),
                GetStringTokenTailDelimiter(TokenType.Await),
                GetStringTokenTailDelimiter(TokenType.Yield),
                GetStringTokenTailDelimiter(TokenType.Break),
                GetStringTokenTailDelimiter(TokenType.If),
                GetStringTokenTailDelimiter(TokenType.Else),
                GetStringTokenTailDelimiter(TokenType.Func),
                GetStringTokenTailDelimiter(TokenType.End),
                GetStringTokenTailDelimiter(TokenType.Let),
                GetStringTokenTailDelimiter("true", TokenType.Bool),
                GetStringTokenTailDelimiter("false", TokenType.Bool),
                GetStringTokenTailDelimiter(TokenType.Return),
                GetStringTokenTailDelimiter(TokenType.While),
                GetStringTokenTailDelimiter(TokenType.For),
                GetStringTokenTailDelimiter(TokenType.ForEach),
                GetStringTokenTailDelimiter(TokenType.In),
                GetFloatLiteralToken().Try(),
                GetIntLiteralToken(),
                GetCharLiteralToken(),
                GetStringLiteralToken(),
                GetCharToken(TokenType.Semicolon),
                GetCharToken(TokenType.LeftCurlyBracket),
                GetCharToken(TokenType.RightCurlyBracket),
                GetCharToken(TokenType.LeftBracket),
                GetCharToken(TokenType.RightBracket),
                GetCharToken(TokenType.LeftParen),
                GetCharToken(TokenType.RightParen),
                GetCharToken(TokenType.DotOp),
                GetCharToken(TokenType.GreaterOp),
                GetCharToken(TokenType.LessOp),
                GetCharToken(TokenType.PlusOp),
                GetCharToken(TokenType.MinusOp),
                GetCharToken(TokenType.MulOp),
                GetCharToken(TokenType.DivOp),
                GetCharToken(TokenType.ModOp),
                GetCharToken(TokenType.PipeOp),
                GetCharToken(TokenType.AssignmentOp),
                GetCharToken(TokenType.Comma),
                GetCharToken(TokenType.NotOp),
                GetIdToken(),
                GetClassNameToken(),
                GetMacroNameToken(),
                GetUnknownToken()
            );
        }

        static public Parse.Parser<Unit> Delimiter()
        {
            return
                Parse.Or(
                    Parse.End.Map(_ => Unit.Value),
                    Skip(),
                    Parse.Verify(c => char.IsLetterOrDigit(c) == false && c != '_').Map(_ => Unit.Value).NoConsume()
                );
        }

        static public Parse.Parser<Unit> Skip()
        {
            return Parse.Or(CommentOut(), SkipChar());
        }

        static private Parse.Parser<Unit> SkipChar()
        {
            return
                Parse.Or(
                    Parse.Char(' '),
                    Parse.Char('\n'),
                    Parse.Char('\r'),
                    Parse.Char('\t')
                )
                .Map(_ => Unit.Value);
        }

        static public Parse.Parser<Unit> CommentOut()
        {
            return Parse.Or(LineCommentOut().Try(), RangeCommentOut().Try());
        }

        static private Parse.Parser<Unit> LineCommentOut()
        {
            return
                 Parse.String("//")
                 .Right(Parse.Until(Parse.Any, Parse.Char('\n')))
                 .Map(_ => Unit.Value);
        }

        static private Parse.Parser<Unit> RangeCommentOut()
        {
            return
                 Parse.String("/*")
                 .Right(Parse.Until(Parse.Any, Parse.String("*/")))
                 .Map(_ => Unit.Value);
        }

        static public Parse.Parser<Unit> OneManySkip()
        {
            return Parse.OneMany(Skip()).Map(_ => Unit.Value);
        }

        static public Parse.Parser<Unit> ManySkip()
        {
            return Parse.Many(Skip()).Map(_ => Unit.Value);
        }

        static public Parse.Parser<Token> GetCharToken(TokenType tokenType)
        {
            return GetCharToken(tokenType.GetText()[0], tokenType);
        }

        static public Parse.Parser<Token> GetCharToken(char c, TokenType tokenType)
        {
            return
                from position in Parse.Positioned
                from character in Parse.Char(c)
                select new Token(tokenType, character.ToString(), position.Start);
        }

        static public Parse.Parser<Token> GetStringToken(TokenType tokenType)
        {
            return GetStringToken(tokenType.GetText(), tokenType);
        }

        static public Parse.Parser<Token> GetStringToken(string str, TokenType tokenType)
        {
            return
                from position in Parse.Positioned
                from text in Parse.String(str)
                select new Token(tokenType, text, position.Start);
        }

        static public Parse.Parser<Token> GetStringTokenTailDelimiter(TokenType tokenType)
        {
            return GetStringTokenTailDelimiter(tokenType.GetText(), tokenType);
        }

        static public Parse.Parser<Token> GetStringTokenTailDelimiter(string str, TokenType tokenType)
        {
            return GetStringToken(str, tokenType).Left(Delimiter()).Try();
        }

        static public Parse.Parser<Token> GetIdToken()
        {
            return
                from position in Parse.Positioned
                from firstChar in Parse.Verify(LexerHelper.IsLowerLetter)
                from tailStr in Parse.Many(Parse.Verify(LexerHelper.IsIdChar)).Text()
                select new Token(TokenType.Id, firstChar.ToString() + tailStr, position.Start);
        }

        static public Parse.Parser<Token> GetClassNameToken()
        {
            return
                from position in Parse.Positioned
                from firstChar in Parse.Verify(LexerHelper.IsUpperLetter)
                from tailStr in Parse.Many(Parse.Verify(c => char.IsLetter(c) || char.IsDigit(c))).Text()
                select new Token(TokenType.ClassName, firstChar.ToString() + tailStr, position.Start);
        }

        static public Parse.Parser<Token> GetMacroNameToken()
        {
            return
                from position in Parse.Positioned
                from firstChar in Parse.Char('#')
                from tailStr in Parse.Many(Parse.Verify(c => char.IsLetter(c) || char.IsDigit(c))).Text()
                select new Token(TokenType.MacroName, firstChar.ToString() + tailStr, position.Start);
        }

        static public Parse.Parser<Token> GetIntLiteralToken()
        {
            return
                from position in Parse.Positioned
                from text in Parse.OneMany(Parse.Verify(char.IsDigit)).Text()
                select new Token(TokenType.Int, text, position.Start);
        }

        static public Parse.Parser<Token> GetCharLiteralToken()
        {
            return
                from position in Parse.Positioned
                from _ in Parse.Char('\'')
                from text in Parse.Or(GetEscapeChar().Try(), Parse.Any).Left(Parse.Char('\''))
                select new Token(TokenType.Char, "'" + text + "'", position.Start);
        }

        static public Parse.Parser<Token> GetStringLiteralToken()
        {
            return
                from position in Parse.Positioned
                from _ in Parse.Char('"')
                from text in Parse.Until(Parse.Or(GetEscapeChar().Try(), Parse.Any), Parse.Char('"')).Text()
                select new Token(TokenType.String, "\"" + text + "\"", position.Start);
        }

        static public Parse.Parser<char> GetEscapeChar()
        {
            return
                from value in Parse.Current.Where(c => c == '\\')
                from escapeChar in Parse.Current.Map(c => LexerHelper.ToEspaceChar(value.ToString() + c)).Where(x => x.HasValue)
                select escapeChar.Value;
        }

        static public Parse.Parser<Token> GetFloatLiteralToken()
        {
            return
                from position in Parse.Positioned
                from text1 in GetIntLiteralToken().Map(t => t.text)
                from _ in Parse.Char('.')
                from text2 in GetIntLiteralToken().Map(t => t.text)
                select new Token(TokenType.Float, text1 + '.' + text2, position.Start);
        }

        static public Parse.Parser<Token> GetUnknownToken()
        {
            return
                from position in Parse.Positioned
                from text in Parse.Until(Parse.Any, OneManySkip()).Text()
                select new Token(TokenType.UnKnown, text, position.Start);
        }
    }
}
