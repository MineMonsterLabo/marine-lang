using System.Collections.Generic;

namespace MarineLang
{
    public class Parser
    {
        public ParseResult<ProgramAst> Parse(TokenStream stream)
        {
            stream.MoveNext();
            var funcDefinitionAstList = new List<FuncDefinitionAst>();
            while (stream.IsEnd == false)
            {
                var parseResult = ParseFuncDefinition(stream);
                if (parseResult.isError)
                {
                    return ParseResult<ProgramAst>.Error("");
                }
                funcDefinitionAstList.Add(parseResult.ast);
            }

            return ParseResult<ProgramAst>.Success(
                new ProgramAst { funcDefinitionAsts = funcDefinitionAstList.ToArray() }
            );
        }

        ParseResult<FuncDefinitionAst> ParseFuncDefinition(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Func)
                return ParseResult<FuncDefinitionAst>.Error("");
            var backUpIndex = stream.Index;

            if (stream.MoveNext() && stream.Current.tokenType == TokenType.Id)
            {
                var funcName = stream.Current.text;
                if (stream.MoveNext())
                {
                    var paramListResult = ParseParamList(stream);

                    if (paramListResult.isError)
                        return ParseResult<FuncDefinitionAst>.Error("");

                    var funcCallAstList = new List<FuncCallAst>();
                    while (stream.IsEnd == false && stream.Current.tokenType != TokenType.End)
                    {

                        var parseResult = ParseFuncCall(stream);
                        if (parseResult.isError)
                        {
                            stream.SetIndex(backUpIndex);
                            return ParseResult<FuncDefinitionAst>.Error("");
                        }
                        funcCallAstList.Add(parseResult.ast);
                    }
                    stream.MoveNext();

                    return ParseResult<FuncDefinitionAst>.Success(
                        new FuncDefinitionAst { funcName = funcName, statementAsts = funcCallAstList.ToArray() }
                    );
                }
            }

            stream.SetIndex(backUpIndex);
            return ParseResult<FuncDefinitionAst>.Error("");
        }

        ParseResult<FuncCallAst> ParseFuncCall(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Id)
                return ParseResult<FuncCallAst>.Error("");

            var funcName = stream.Current.text;

            var backUpIndex = stream.Index;

            if (stream.MoveNext())
            {
                var paramListResult = ParseParamList(stream);

                if (paramListResult.isError == false)
                    return ParseResult<FuncCallAst>.Success(new FuncCallAst { funcName = funcName });
            }

            stream.SetIndex(backUpIndex);
            return ParseResult<FuncCallAst>.Error("");
        }

        ParseResult<Token[]> ParseParamList(TokenStream stream)
        {
            var backUpIndex = stream.Index;

            if (stream.Current.tokenType == TokenType.LeftParen)
            {
                if (stream.MoveNext() && stream.Current.tokenType == TokenType.RightParen)
                {
                    stream.MoveNext();
                    return ParseResult<Token[]>.Success(new Token[] { });
                }
            }
            stream.SetIndex(backUpIndex);
            return ParseResult<Token[]>.Error("");
        }
    }
}
