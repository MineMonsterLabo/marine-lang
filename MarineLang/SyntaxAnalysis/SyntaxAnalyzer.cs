using MarineLang.Models;
using MarineLang.Streams;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class SyntaxAnalyzer
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
            var headToken = stream.Current;
            return
                ParseToken(TokenType.Func)
                .InCompleteErrorWithPositionEnd($"関数定義が間違っています \"{stream.Current.text}\"", ErrorCode.NonFuncWord)
                .Right(ParseToken(TokenType.Id))
                .InCompleteError($"関数定義に関数名がありません", ErrorCode.NonFuncName, headToken.PositionEnd)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionEnd("関数定義には()が必要です", ErrorCode.NonFuncParen)
                .Bind(funcNameToken =>
                     ParserCombinator.Try(ParseVariableList)
                     .InCompleteError("関数定義には()が必要です", ErrorCode.NonFuncParen, funcNameToken.PositionEnd)
                     .Bind(varList =>
                        ParserCombinator.Try(ParseFuncBody)
                        .MapResult(statementAsts => FuncDefinitionAst.Create(funcNameToken.text, varList, statementAsts))
                     )
                 )
                .Left(ParseToken(TokenType.End))
                 .InCompleteErrorWithPositionEnd($"関数の終わりにendがありません", ErrorCode.NonEndWord)
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

            return ParseResult<StatementAst[]>.CreateSuccess(statementAsts.ToArray());
        }

        Parser<ExprAst> ParseExpr()
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
                );
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
                .Right(ParseExpr().InCompleteErrorWithPositionHead("retの後には式が必要です", ErrorCode.NonRetExpr, ErrorKind.ForceError))
                .MapResult(ReturnAst.Create)
                (stream);
        }

        IParseResult<AssignmentAst> ParseAssignment(TokenStream stream)
        {
            return
                ParseToken(TokenType.Let)
                .Right(
                    ParseToken(TokenType.Id)
                    .InCompleteErrorWithPositionHead("letの後には変数名が必要です", ErrorCode.NonLetVarName, ErrorKind.ForceError)
                )
                .Left(
                    ParseToken(TokenType.AssignmentOp)
                    .InCompleteErrorWithPositionHead("letに=がありません", ErrorCode.NonLetEqual, ErrorKind.ForceError)
                 )
                .Bind(varNameToken =>
                    ParseExpr()
                    .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
                    .MapResult(expr => AssignmentAst.Create(varNameToken.text, expr))
                )
                (stream);
        }

        IParseResult<ReAssignmentAst> ParseReAssignment(TokenStream stream)
        {
            return
               ParseToken(TokenType.Id)
               .Left(ParseToken(TokenType.AssignmentOp))
               .Bind(varNameToken =>
                    ParseExpr()
                    .MapResult(expr => ReAssignmentAst.Create(varNameToken.text, expr))
                    .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
               )
               (stream);
        }

        IParseResult<ValueAst> ParseInt(TokenStream stream)
        {
            return ParseToken(TokenType.Int)
                .BindResult(token =>
                 (int.TryParse(token.text, out int value)) ?
                     ParseResult<ValueAst>.CreateSuccess(ValueAst.Create(value)) :
                     ParseResult<ValueAst>.CreateError(new Error())
             )(stream);
        }

        IParseResult<ValueAst> ParseFloat(TokenStream stream)
        {
            return ParseToken(TokenType.Float)
               .BindResult(token =>
                    (float.TryParse(token.text, out float value)) ?
                        ParseResult<ValueAst>.CreateSuccess(ValueAst.Create(value)) :
                        ParseResult<ValueAst>.CreateError(new Error())
                )(stream);
        }

        IParseResult<ValueAst> ParseBool(TokenStream stream)
        {
            return ParseToken(TokenType.Bool)
                .MapResult(token => ValueAst.Create(bool.Parse(token.text)))
                (stream);
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

        Parser<Token> ParseToken(TokenType tokenType)
        {
            return ParserCombinator.TestOnce(token => token.tokenType == tokenType);
        }
    }
}
