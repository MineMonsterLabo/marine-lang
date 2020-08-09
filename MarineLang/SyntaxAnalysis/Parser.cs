using MarineLang.Models;
using MarineLang.Streams;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class Parser
    {
        public ParseResult<ProgramAst> Parse(TokenStream stream)
        {
            stream.MoveNext();

            return
                ParserCombinator.Many(
                    ParserCombinator.Try(ParseFuncDefinition)
                )(stream)
                .Map(Enumerable.ToArray)
                .Map(ProgramAst.Create);
        }

        ParseResult<FuncDefinitionAst> ParseFuncDefinition(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Func)
                return ParseResult<FuncDefinitionAst>.Error("");

            if (stream.MoveNext() && stream.Current.tokenType == TokenType.Id)
            {
                var funcName = stream.Current.text;
                if (stream.MoveNext())
                {
                    var paramListResult = ParserCombinator.Try(ParseParamList)(stream);

                    if (paramListResult.isError)
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

        ParseResult<StatementAst[]> ParseFuncBody(TokenStream stream)
        {
            var statementAsts = new List<StatementAst>();
            while (stream.IsEnd == false && stream.Current.tokenType != TokenType.End)
            {
                var parseResult =
                    ParserCombinator.Try(ParseFuncCall)(stream).Cast<StatementAst>()
                    .ErrorReplace(
                        ParserCombinator.Try(ParseReturn)(stream).Cast<StatementAst>()
                    );

                if (parseResult.isError)
                {
                    return parseResult.CastError<StatementAst[]>();
                }

                statementAsts.Add(parseResult.value);
            }
            stream.MoveNext();

            return ParseResult<StatementAst[]>.Success(statementAsts.ToArray());
        }

        ParseResult<FuncCallAst> ParseFuncCall(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Id)
                return ParseResult<FuncCallAst>.Error("");

            var funcName = stream.Current.text;

            if (stream.MoveNext())
            {
                var paramListResult = ParserCombinator.Try(ParseParamList)(stream);

                if (paramListResult.isError == false)
                    return ParseResult<FuncCallAst>.Success(new FuncCallAst { funcName = funcName });
            }

            return ParseResult<FuncCallAst>.Error("");
        }

        ParseResult<ReturnAst> ParseReturn(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Return)
                return ParseResult<ReturnAst>.Error("");

            if (stream.MoveNext())
                return
                    ParseInt(stream)
                    .Map(value => new ReturnAst { value = value });

            return ParseResult<ReturnAst>.Error("");
        }

        ParseResult<int> ParseInt(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Int)
                return ParseResult<int>.Error("");
            if (int.TryParse(stream.Current.text, out int value))
            {
                stream.MoveNext();
                return ParseResult<int>.Success(value);
            }
            return ParseResult<int>.Error("");
        }

        ParseResult<Token[]> ParseParamList(TokenStream stream)
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
