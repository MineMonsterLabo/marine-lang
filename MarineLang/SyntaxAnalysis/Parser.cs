using MarineLang.Models;
using MarineLang.Streams;
using System;
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
            return
                ParseToken(TokenType.Func)
                .Error($"関数定義が間違っています \"{stream.Current.text}\"", position)
                .Right(ParseToken(TokenType.Id).Error($"関数定義に関数名がありません ", position))
                .Bind(funcNameToken =>
                     ParserCombinator.Try(ParseVariableList)
                        .Bind(varList =>
                            ParserCombinator.Try(ParseFuncBody)
                            .MapResult(statementAsts => FuncDefinitionAst.Create(funcNameToken.text, varList, statementAsts))
                        )

                 ).Left(ParseToken(TokenType.End).Error($"関数定義にendがありません ", position))
                (stream);
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
                        ParserCombinator.Try(ParseExpr())
                    )(stream);

                if (parseResult.IsError)
                    return parseResult.CastError<StatementAst[]>();

                statementAsts.Add(parseResult.Value);
            }

            return ParseResult<StatementAst[]>.Success(statementAsts.ToArray());
        }

        Func<TokenStream, IParseResult<ExprAst>> ParseExpr()
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
                ).Error("式が必要です");
        }

        IParseResult<FuncCallAst> ParseFuncCall(TokenStream stream)
        {
            return
                ParseToken(TokenType.Id)
                .Bind(funcNameToken =>
                    ParserCombinator.Try(ParseParamList)
                    .MapResult(paramList =>
                        new FuncCallAst { funcName = funcNameToken.text, args = paramList }
                    )
                )(stream);
        }

        IParseResult<ReturnAst> ParseReturn(TokenStream stream)
        {
            return ParseToken(TokenType.Return)
                .Right(ParseExpr())
                .MapResult(ReturnAst.Create)
                (stream);
        }

        IParseResult<AssignmentAst> ParseAssignment(TokenStream stream)
        {
            return
                ParseToken(TokenType.Let)
                .Right(ParseToken(TokenType.Id))
                .Left(ParseToken(TokenType.AssignmentOp))
                .Bind(varNameToken => ParseExpr().MapResult(expr => AssignmentAst.Create(varNameToken.text, expr)))
                (stream);
        }

        IParseResult<ReAssignmentAst> ParseReAssignment(TokenStream stream)
        {
            var varNameResult =
                ParseToken(TokenType.Id)
                .Left(ParseToken(TokenType.AssignmentOp))
                (stream);

            if (varNameResult.IsError || stream.IsEnd)
                return ParseResult<ReAssignmentAst>.Error("");

            return ParseExpr()(stream)
                .Map(expr =>
                    ReAssignmentAst.Create(varNameResult.Value.text, expr)
                );
        }

        IParseResult<ValueAst> ParseInt(TokenStream stream)
        {
            return ParseToken(TokenType.Int)
                .BindResult(token =>
                 (int.TryParse(token.text, out int value)) ?
                     ParseResult<ValueAst>.Success(ValueAst.Create(value)) :
                     ParseResult<ValueAst>.Error("")
             )(stream);
        }

        IParseResult<ValueAst> ParseFloat(TokenStream stream)
        {
            return ParseToken(TokenType.Float)
               .BindResult(token =>
                    (float.TryParse(token.text, out float value)) ?
                        ParseResult<ValueAst>.Success(ValueAst.Create(value)) :
                        ParseResult<ValueAst>.Error("")
                )(stream);
        }

        IParseResult<ValueAst> ParseBool(TokenStream stream)
        {
            return ParseToken(TokenType.Bool)
                .BindResult(token =>
                    (bool.TryParse(token.text, out bool value)) ?
                        ParseResult<ValueAst>.Success(ValueAst.Create(value)) :
                        ParseResult<ValueAst>.Error("")
                )(stream);
        }

        IParseResult<ValueAst> ParseChar(TokenStream stream)
        {
            return ParseToken(TokenType.Char)
              .MapResult(token => ValueAst.Create(token.text[1]))
              (stream);
        }

        IParseResult<ValueAst> ParseString(TokenStream stream)
        {
            return ParseToken(TokenType.String)
            .MapResult(token => token.text)
            .MapResult(text => text.Length == 2 ? "" : text.Substring(1, text.Length - 2))
            .MapResult(ValueAst.Create)
            (stream);
        }

        IParseResult<VariableAst> ParseVariable(TokenStream stream)
        {
            return
                ParseToken(TokenType.Id)
                .MapResult(token => VariableAst.Create(token.text))
                (stream);
        }

        IParseResult<ExprAst[]> ParseParamList(TokenStream stream)
        {
            return
               ParseToken(TokenType.LeftParen)
               .Right(ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma)))
               .Left(ParseToken(TokenType.RightParen))
               (stream);
        }

        IParseResult<VariableAst[]> ParseVariableList(TokenStream stream)
        {
            return
                ParseToken(TokenType.LeftParen)
                .Right(ParserCombinator.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.RightParen))
                (stream);
        }

        Func<TokenStream, IParseResult<Token>> ParseToken(TokenType tokenType)
        {
            return stream =>
            {
                if (stream.Current.tokenType == tokenType)
                {
                    var token = stream.Current;
                    stream.MoveNext();
                    return ParseResult<Token>.Success(token);
                }
                return ParseResult<Token>.Error("");
            };
        }
    }
}
