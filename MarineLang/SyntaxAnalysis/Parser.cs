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
                        .Map(funcCallAstList =>
                             FuncDefinitionAst.Create(funcName, funcCallAstList)
                        );
                }
            }

            return ParseResult<FuncDefinitionAst>.Error("");
        }

        ParseResult<FuncCallAst[]> ParseFuncBody(TokenStream stream)
        {
            var funcCallAstList = new List<FuncCallAst>();
            while (stream.IsEnd == false && stream.Current.tokenType != TokenType.End)
            {
                var parseResult = ParserCombinator.Try(ParseFuncCall)(stream);

                if (parseResult.isError)
                    return parseResult.CastError<FuncCallAst[]>();

                funcCallAstList.Add(parseResult.value);
            }
            stream.MoveNext();

            return ParseResult<FuncCallAst[]>.Success(funcCallAstList.ToArray());
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
