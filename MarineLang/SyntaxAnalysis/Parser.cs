using MarineLang.Models;
using MarineLang.Streams;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class Parser
    {
        public IParseResult<ProgramAst> Parse(TokenStream stream)
        {
            stream.MoveNext();

            return
                ParserCombinator.Many(
                    ParserCombinator.Try(ParseFuncDefinition)
                )(stream)
                .Map(Enumerable.ToArray)
                .Map(ProgramAst.Create);
        }

        IParseResult<FuncDefinitionAst> ParseFuncDefinition(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Func)
                return ParseResult<FuncDefinitionAst>.Error("");

            if (stream.MoveNext() && stream.Current.tokenType == TokenType.Id)
            {
                var funcName = stream.Current.text;
                if (stream.MoveNext())
                {
                    var paramListResult = ParserCombinator.Try(ParseParamList)(stream);

                    if (paramListResult.IsError)
                        return paramListResult.CastError<FuncDefinitionAst>();

                    return
                        ParserCombinator.Try(ParseFuncBody)(stream)
                        .Map(statementAsts =>
                             FuncDefinitionAst.Create(funcName, statementAsts)
                        );
                }
            }

            return ParseResult<FuncDefinitionAst>.Error("");
        }

        IParseResult<StatementAst[]> ParseFuncBody(TokenStream stream)
        {
            var statementAsts = new List<StatementAst>();
            while (stream.IsEnd == false && stream.Current.tokenType != TokenType.End)
            {
                var parseResult =
                    ParserCombinator.Or<StatementAst>(
                        ParserCombinator.Try(ParseExpr),
                        ParserCombinator.Try(ParseReturn)
                    )(stream);

                if (parseResult.IsError)
                    return parseResult.CastError<StatementAst[]>();

                statementAsts.Add(parseResult.Value);
            }
            stream.MoveNext();

            return ParseResult<StatementAst[]>.Success(statementAsts.ToArray());
        }

        IParseResult<ExprAst> ParseExpr(TokenStream stream)
        {
            return
                ParserCombinator.Or<ExprAst>(
                    ParserCombinator.Try(ParseFuncCall),
                    ParserCombinator.Try(ParseInt),
                    ParserCombinator.Try(ParseBool),
                    ParserCombinator.Try(ParseChar),
                    ParserCombinator.Try(ParseString)
                )(stream);
        }

        IParseResult<FuncCallAst> ParseFuncCall(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Id)
                return ParseResult<FuncCallAst>.Error("");

            var funcName = stream.Current.text;

            if (stream.MoveNext())
            {
                var paramListResult = ParserCombinator.Try(ParseParamList)(stream);

                if (paramListResult.IsError == false)
                    return ParseResult<FuncCallAst>.Success(new FuncCallAst { funcName = funcName });
            }

            return ParseResult<FuncCallAst>.Error("");
        }

        IParseResult<ReturnAst> ParseReturn(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Return)
                return ParseResult<ReturnAst>.Error("");

            if (stream.MoveNext())
                return
                    ParseExpr(stream)
                    .Map(ReturnAst.Create);

            return ParseResult<ReturnAst>.Error("");
        }

        IParseResult<ValueAst> ParseInt(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Int)
                return ParseResult<ValueAst>.Error("");
            if (int.TryParse(stream.Current.text, out int value))
            {
                stream.MoveNext();
                return ParseResult<ValueAst>.Success(ValueAst.Create(value));
            }
            return ParseResult<ValueAst>.Error("");
        }

        IParseResult<ValueAst> ParseBool(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Bool)
                return ParseResult<ValueAst>.Error("");
            if (bool.TryParse(stream.Current.text, out bool value))
            {
                stream.MoveNext();
                return ParseResult<ValueAst>.Success(ValueAst.Create(value));
            }
            return ParseResult<ValueAst>.Error("");
        }

        IParseResult<ValueAst> ParseChar(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Char)
                return ParseResult<ValueAst>.Error("");
            var value = stream.Current.text[1];
            stream.MoveNext();
            return
                ParseResult<ValueAst>.Success(
                    ValueAst.Create(value)
                );
        }

        IParseResult<ValueAst> ParseString(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.String)
                return ParseResult<ValueAst>.Error("");
            var text = stream.Current.text;
            var value = text.Length == 2 ? "" : text.Substring(1, text.Length - 2);
            stream.MoveNext();
            return
                ParseResult<ValueAst>.Success(
                    ValueAst.Create(value)
                );
        }

        IParseResult<Token[]> ParseParamList(TokenStream stream)
        {
            if (stream.Current.tokenType == TokenType.LeftParen)
            {
                if (stream.MoveNext() && stream.Current.tokenType == TokenType.RightParen)
                {
                    stream.MoveNext();
                    return ParseResult<Token[]>.Success(new Token[] { });
                }
            }
            return ParseResult<Token[]>.Error("");
        }
    }
}
