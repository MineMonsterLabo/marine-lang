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
            var position = stream.Current.position;

            if (stream.Current.tokenType != TokenType.Func)
                return ParseResult<FuncDefinitionAst>
                    .Error($"関数定義が間違っています \"{stream.Current.text}\"", position);

            if (stream.MoveNext() && stream.Current.tokenType == TokenType.Id)
            {
                var funcName = stream.Current.text;
                if (stream.MoveNext())
                {
                    var variableListResult = ParserCombinator.Try(ParseVariableList)(stream);

                    if (variableListResult.IsError)
                        return variableListResult.CastError<FuncDefinitionAst>();

                    var funcDefinitionResult =
                        ParserCombinator.Try(ParseFuncBody)(stream)
                        .Map(statementAsts =>
                             FuncDefinitionAst.Create(funcName, variableListResult.Value, statementAsts)
                        );

                    if (funcDefinitionResult.IsError)
                        return funcDefinitionResult;

                    if (stream.IsEnd == true || stream.Current.tokenType != TokenType.End)
                        return
                             ParseResult<FuncDefinitionAst>.Error($"関数定義にendがありません ", position);

                    stream.MoveNext();

                    return funcDefinitionResult;

                }
            }

            return
                ParseResult<FuncDefinitionAst>.Error($"関数定義に関数名がありません ", position);
        }

        IParseResult<StatementAst[]> ParseFuncBody(TokenStream stream)
        {
            var statementAsts = new List<StatementAst>();
            while (stream.IsEnd == false && stream.Current.tokenType != TokenType.End)
            {
                var parseResult =
                    ParserCombinator.Or<StatementAst>(
                        ParserCombinator.Try(ParseReturn),
                        ParserCombinator.Try(ParseAssignment),
                        ParserCombinator.Try(ParseReAssignment),
                        ParserCombinator.Try(ParseExpr)
                    )(stream);

                if (parseResult.IsError)
                    return parseResult.CastError<StatementAst[]>();

                statementAsts.Add(parseResult.Value);
            }

            return ParseResult<StatementAst[]>.Success(statementAsts.ToArray());
        }

        IParseResult<ExprAst> ParseExpr(TokenStream stream)
        {
            return
                ParserCombinator.Or<ExprAst>(
                    ParserCombinator.Try(ParseFuncCall),
                    ParserCombinator.Try(ParseFloat),
                    ParserCombinator.Try(ParseInt),
                    ParserCombinator.Try(ParseBool),
                    ParserCombinator.Try(ParseChar),
                    ParserCombinator.Try(ParseString),
                    ParserCombinator.Try(ParseVariable)
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
                    return ParseResult<FuncCallAst>.Success(
                        new FuncCallAst
                        {
                            funcName = funcName,
                            args = paramListResult.Value
                        }
                    );
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

        IParseResult<AssignmentAst> ParseAssignment(TokenStream stream)
        {
            if (ParseToken(stream, TokenType.Let).IsError || stream.IsEnd)
                return ParseResult<AssignmentAst>.Error("");

            var varNameResult = ParseToken(stream, TokenType.Id);
            if (varNameResult.IsError || stream.IsEnd)
                return ParseResult<AssignmentAst>.Error("");
            if (ParseToken(stream, TokenType.AssignmentOp).IsError || stream.IsEnd)
                return ParseResult<AssignmentAst>.Error("");
            var exprResult = ParseExpr(stream);
            if (exprResult.IsError)
                return ParseResult<AssignmentAst>.Error("");

            return ParseResult<AssignmentAst>.Success(
                AssignmentAst.Create(varNameResult.Value.text, exprResult.Value)
            );
        }

        IParseResult<ReAssignmentAst> ParseReAssignment(TokenStream stream)
        {
            var varNameResult = ParseToken(stream, TokenType.Id);
            if (varNameResult.IsError || stream.IsEnd)
                return ParseResult<ReAssignmentAst>.Error("");
            if (ParseToken(stream, TokenType.AssignmentOp).IsError || stream.IsEnd)
                return ParseResult<ReAssignmentAst>.Error("");
            var exprResult = ParseExpr(stream);
            if (exprResult.IsError)
                return ParseResult<ReAssignmentAst>.Error("");

            return ParseResult<ReAssignmentAst>.Success(
                ReAssignmentAst.Create(varNameResult.Value.text, exprResult.Value)
            );
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

        IParseResult<ValueAst> ParseFloat(TokenStream stream)
        {
            if (stream.Current.tokenType != TokenType.Float)
                return ParseResult<ValueAst>.Error("");
            if (float.TryParse(stream.Current.text, out float value))
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

        IParseResult<VariableAst> ParseVariable(TokenStream stream)
        {
            var variableResult = ParseToken(stream, TokenType.Id);
            if (variableResult.IsError)
                return ParseResult<VariableAst>.Error("");

            return
                ParseResult<VariableAst>.Success(
                    VariableAst.Create(variableResult.Value.text)
                );
        }

        IParseResult<ExprAst[]> ParseParamList(TokenStream stream)
        {
            if (ParseToken(stream, TokenType.LeftParen).IsError == false)
            {
                var exprListResult
                    = ParserCombinator.Separated(ParseExpr, stream2 => ParseToken(stream2, TokenType.Comma))
                    (stream);
                if (exprListResult.IsError == false)
                    if (ParseToken(stream, TokenType.RightParen).IsError == false)
                        return ParseResult<ExprAst[]>.Success(exprListResult.Value);
            }
            return ParseResult<ExprAst[]>.Error("");
        }

        IParseResult<VariableAst[]> ParseVariableList(TokenStream stream)
        {
            if (ParseToken(stream, TokenType.LeftParen).IsError == false)
            {
                var exprListResult
                    = ParserCombinator.Separated(ParseVariable, stream2 => ParseToken(stream2, TokenType.Comma))
                    (stream);
                if (exprListResult.IsError == false)
                    if (ParseToken(stream, TokenType.RightParen).IsError == false)
                        return ParseResult<VariableAst[]>.Success(exprListResult.Value);
            }
            return ParseResult<VariableAst[]>.Error("");
        }

        IParseResult<Token> ParseToken(TokenStream stream, TokenType tokenType)
        {
            if (stream.Current.tokenType == tokenType)
            {
                var token = stream.Current;
                stream.MoveNext();
                return ParseResult<Token>.Success(token);
            }
            return ParseResult<Token>.Error("");
        }
    }
}
