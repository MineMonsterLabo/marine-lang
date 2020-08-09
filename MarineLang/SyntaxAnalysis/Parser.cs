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
                    ParserCombinator.Try(ParseInt)
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

        IParseResult<ValueAst<int>> ParseInt(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Int)
                return ParseResult<ValueAst<int>>.Error("");
            if (int.TryParse(stream.Current.text, out int value))
            {
                stream.MoveNext();
                return ParseResult<ValueAst<int>>.Success(ValueAst<int>.Create(value));
            }
            return ParseResult<ValueAst<int>>.Error("");
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
