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
                .InCompleteErrorWithPositionHead($"関数定義が間違っています \"{stream.Current.text}\"", ErrorCode.NonFuncWord)
                .Right(ParseToken(TokenType.Id))
                .InCompleteError($"関数定義に関数名がありません", ErrorCode.NonFuncName, headToken.PositionEnd)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionEnd("関数定義には()が必要です", ErrorCode.NonFuncParen)
                .Bind(funcNameToken =>
                     ParserCombinator.Try(ParseVariableList)
                     .InCompleteError("関数定義には()が必要です", ErrorCode.NonFuncParen, funcNameToken.PositionEnd)
                     .Bind(varList =>
                        ParserCombinator.Try(ParseFuncBody(TokenType.End))
                        .MapResult(statementAsts => FuncDefinitionAst.Create(funcNameToken.text, varList, statementAsts))
                     )
                 )
                .Left(ParseToken(TokenType.End))
                 .InCompleteErrorWithPositionEnd($"関数の終わりにendがありません", ErrorCode.NonEndWord)
                (stream);
        }

        Parser<StatementAst[]> ParseFuncBody(TokenType endToken)
        {
            return stream =>
            {
                var statementAsts = new List<StatementAst>();
                while (stream.IsEnd == false && stream.Current.tokenType != endToken)
                {
                    var parseResult = ParseStatement()(stream);

                    if (parseResult.IsError)
                        return parseResult.CastError<StatementAst[]>();

                    statementAsts.Add(parseResult.Value);
                }

                return ParseResult<StatementAst[]>.CreateSuccess(statementAsts.ToArray());
            };
        }

        Parser<StatementAst> ParseStatement()
        {
            return
                ParserCombinator.Or(
                    ParserCombinator.Try(ParseYield()),
                    ParserCombinator.Try(ParseWhile()),
                    ParserCombinator.Try(ParseFor()),
                    ParserCombinator.Try(ParseReturn),
                    ParserCombinator.Try(ParseAssignmentVariable),
                    ParserCombinator.Try(ParseFieldAssignment),
                    ParserCombinator.Try(ParseReAssignmentVariable),
                    ParserCombinator.Try(ParseReAssignmentIndexer),
                    ParserCombinator.Try(ParseExpr())
                );
        }

        Parser<YieldAst> ParseYield()
        {
            return ParseToken(TokenType.Yield).MapResult(_ => new YieldAst());
        }

        Parser<WhileAst> ParseWhile()
        {
            var conditionExprParser =
                ParseToken(TokenType.While)
                .Right(ParseExpr());
            var blockParser = ParseBlock();

            return ParserCombinator.Tuple(conditionExprParser, blockParser)
                 .MapResult(pair => WhileAst.Create(pair.Item1, pair.Item2));
        }

        Parser<ForAst> ParseFor()
        {
            var initVariableParser =
                    ParseToken(TokenType.For)
                    .Right(ParseVariable())
                    .Left(ParseToken(TokenType.AssignmentOp));

            var initExprParser = ParseExpr();

            var maxValueParser =
                ParseToken(TokenType.Comma)
                .Right(ParseExpr());

            var addValueParser =
                ParseToken(TokenType.Comma)
                .Right(ParseExpr());

            var blockParser = ParseBlock();

            return
                ParserCombinator.Tuple(initVariableParser, initExprParser, maxValueParser, addValueParser, blockParser)
                .MapResult(tuple => ForAst.Create(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5));
        }

        Parser<ExprAst> ParseExpr()
        {
            return stream =>
                ParserCombinator.Or(
                    ParseIfExpr(),
                    ParseBinaryOpExpr()
                )(stream);
        }

        Parser<ExprAst> ParseIfExpr()
        {
            return
                ParseToken(TokenType.If)
                .Right(ParseExpr())
                .Bind(condExpr =>
                    ParseBlock()
                    .Bind<StatementAst[], IfExprAst>(thenExpr =>
                         stream =>
                         {
                             var elseExprResult = ParserCombinator.Try(
                                 ParseToken(TokenType.Else)
                                 .Right(ParseBlock())
                                 )(stream);
                             if (elseExprResult.IsError)
                                 return ParseResult<IfExprAst>.CreateSuccess(
                                        IfExprAst.Create(condExpr, thenExpr, new StatementAst[] { })
                                     );
                             return ParseResult<IfExprAst>.CreateSuccess(
                                    IfExprAst.Create(condExpr, thenExpr, elseExprResult.Value)
                                 );
                         }
                    )
               );
        }

        Parser<StatementAst[]> ParseBlock()
        {
            return stream =>
            {
                if (ParseToken(TokenType.LeftCurlyBracket)(stream).IsError || stream.IsEnd)
                    return ParseResult<StatementAst[]>.CreateError(new Error("", ErrorKind.InComplete));

                var statementAsts = new List<StatementAst>();
                while (stream.IsEnd == false && stream.Current.tokenType != TokenType.RightCurlyBracket)
                {
                    var parseResult = ParseStatement()(stream);

                    if (parseResult.IsError)
                        return parseResult.CastError<StatementAst[]>();

                    statementAsts.Add(parseResult.Value);
                }

                if (stream.IsEnd || ParseToken(TokenType.RightCurlyBracket)(stream).IsError)
                    return ParseResult<StatementAst[]>.CreateError(new Error("", ErrorKind.InComplete));

                return ParseResult<StatementAst[]>.CreateSuccess(statementAsts.ToArray());
            };
        }

        Parser<ExprAst> ParseBinaryOpExpr()
        {
            return stream =>
            {
                var exprResult = ParseUnaryOpExpr()(stream);
                if (exprResult.IsError || stream.IsEnd)
                    return exprResult;

                var opResult = ParseBinaryOpToken()(stream);
                if (opResult.IsError)
                    return exprResult;
                return
                    ParseBinaryOp2Expr(exprResult.Value, opResult.Value.tokenType)(stream);
            };
        }
        Parser<ExprAst> ParseBinaryOp2Expr(ExprAst beforeExpr, TokenType beforeTokenType)
        {
            return stream =>
            {
                var exprResult = ParseUnaryOpExpr()(stream);
                if (exprResult.IsError || stream.IsEnd)
                    return exprResult;

                var opResult = ParseBinaryOpToken()(stream);
                if (opResult.IsError)
                    return ParseResult<ExprAst>.CreateSuccess(
                        BinaryOpAst.Create(beforeExpr, exprResult.Value, beforeTokenType)
                    );
                var tokenType = opResult.Value.tokenType;
                if (GetBinaryOpPriority(beforeTokenType) >= GetBinaryOpPriority(tokenType))
                    return
                        ParseBinaryOp2Expr(BinaryOpAst.Create(beforeExpr, exprResult.Value, beforeTokenType), tokenType)(stream);
                return
                    ParseBinaryOp2Expr(exprResult.Value, tokenType)
                    .MapResult(expr => BinaryOpAst.Create(beforeExpr, expr, beforeTokenType))
                    (stream);
            };
        }

        Parser<ExprAst> ParseUnaryOpExpr()
        {
            return
                ParserCombinator.Tuple(
                    ParserCombinator.Many(ParseUnaryOpToken()),
                    ParseDotOpExpr()
                )
                .MapResult(pair =>
                {
                    pair.Item1.Reverse();
                    return pair.Item1.Aggregate(
                        pair.Item2,
                        (expr, unaryOpToken) => UnaryOpAst.Create(expr, unaryOpToken.tokenType)
                    );
                });
        }

        Parser<Token> ParseUnaryOpToken()
        {
            return ParserCombinator.Or(ParseToken(TokenType.MinusOp), ParseToken(TokenType.NotOp));
        }

        Parser<ExprAst> ParseDotOpExpr()
        {
            return stream =>
            {
                var termResult = ParseIndexerOpExpr()(stream);
                if (termResult.IsError || stream.IsEnd)
                    return termResult;

                return ParseDotTerms(termResult.Value)(stream);
            };
        }

        Parser<ExprAst> ParseIndexerOpExpr()
        {
            return stream =>
            {
                var termResult = ParseTerm()(stream);
                if (termResult.IsError || stream.IsEnd)
                    return termResult;

                return ParseIndexers(false)
                    .MapResult(indexExprs =>
                        indexExprs.Aggregate(termResult.Value, (acc, x) => GetIndexerAst.Create(acc, x))
                    )(stream);
            };
        }

        Parser<ExprAst> ParseDotTerms(ExprAst instance)
        {
            var dotTermParser = ParseToken(TokenType.DotOp)
                      .Right(
                          ParserCombinator.Or<ExprAst>(
                              ParseToken(TokenType.Await).MapResult(_ => AwaitAst.Create(instance)),
                                  ParserCombinator.Try(ParseFuncCall)
                                      .MapResult(funcCallAst => InstanceFuncCallAst.Create(instance, funcCallAst)),
                                  ParseVariable()
                                      .MapResult(variableAst => InstanceFieldAst.Create(instance, variableAst.varName))
                          )
                      );
            return stream =>
            {
                while (stream.IsEnd == false)
                {
                    var result = dotTermParser(stream);
                    if (result.IsError)
                        if (result.Error.ErrorKind != ErrorKind.InComplete)
                            return result;
                        else break;
                    instance = result.Value;
                    if (stream.IsEnd)
                        break;
                    var result2 = ParseIndexers(false)(stream);
                    if (result2.IsError)
                        return result2.CastError<ExprAst>();
                    instance = result2.Value.Aggregate(instance, (acc, x) => GetIndexerAst.Create(acc, x));
                }
                return ParseResult<ExprAst>.CreateSuccess(instance);
            };
        }

        Parser<Token> ParseBinaryOpToken()
        {
            return
                ParserCombinator.TestOnce(token =>
                    token.tokenType >= TokenType.OrOp
                    && token.tokenType <= TokenType.ModOp
                );
        }

        int GetBinaryOpPriority(TokenType tokenType)
        {
            if (tokenType == TokenType.MinusOp)
                return (int)TokenType.PlusOp;
            if (tokenType == TokenType.NotEqualOp)
                return (int)TokenType.EqualOp;
            if (
                tokenType == TokenType.GreaterOp ||
                tokenType == TokenType.LessOp ||
                tokenType == TokenType.LessEqualOp
            )
                return (int)TokenType.GreaterEqualOp;
            if (tokenType == TokenType.DivOp || tokenType == TokenType.ModOp)
                return (int)TokenType.MulOp;
            return (int)tokenType;
        }

        Parser<ExprAst> ParseTerm()
        {
            return
                ParserCombinator.Or(
                    ParserCombinator.Try(ParseParenExpr()),
                    ParserCombinator.Try(ParseFuncCall),
                    ParserCombinator.Try(ParseFloat),
                    ParserCombinator.Try(ParseInt),
                    ParserCombinator.Try(ParseBool),
                    ParserCombinator.Try(ParseChar),
                    ParserCombinator.Try(ParseString),
                    ParserCombinator.Try(ParseArrayLiteral()),
                    ParserCombinator.Try(ParseActionLiteral()),
                    ParserCombinator.Try(ParseVariable())
                );
        }

        Parser<ExprAst> ParseParenExpr()
        {
            return
             ParseToken(TokenType.LeftParen)
             .Right(ParseExpr())
             .Left(ParseToken(TokenType.RightParen));
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

        Parser<IReadOnlyList<ExprAst>> ParseIndexers(bool once)
        {
            var parserIndexer = ParseToken(TokenType.LeftBracket)
                .Right(ParseExpr())
                .Left(ParseToken(TokenType.RightBracket));
            return once ? ParserCombinator.OneMany(parserIndexer) : ParserCombinator.Many(parserIndexer);
        }

        IParseResult<ReturnAst> ParseReturn(TokenStream stream)
        {
            return ParseToken(TokenType.Return)
                .InCompleteErrorWithPositionHead("retを期待してます", ErrorCode.Unknown)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionEnd("retの後には式が必要です", ErrorCode.NonRetExpr, ErrorKind.ForceError)
                .Right(ParseExpr().InCompleteErrorWithPositionHead("retの後には式が必要です", ErrorCode.NonRetExpr, ErrorKind.ForceError))
                .MapResult(ReturnAst.Create)
                (stream);
        }

        IParseResult<AssignmentVariableAst> ParseAssignmentVariable
            (TokenStream stream)
        {
            return
                ParseToken(TokenType.Let)
                .InCompleteErrorWithPositionHead("letを期待してます", ErrorCode.Unknown)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionEnd("letの後には変数名が必要です", ErrorCode.NonLetVarName, ErrorKind.ForceError)
                .Right(
                    ParseToken(TokenType.Id)
                    .InCompleteErrorWithPositionHead("letの後には変数名が必要です", ErrorCode.NonLetVarName, ErrorKind.ForceError)
                )
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionEnd("letに=がありません", ErrorCode.NonLetEqual, ErrorKind.ForceError)
                .Left(
                    ParseToken(TokenType.AssignmentOp)
                    .InCompleteErrorWithPositionHead("letに=がありません", ErrorCode.NonLetEqual, ErrorKind.ForceError)
                 )
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
                .Bind(varNameToken =>
                    ParseExpr()
                    .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
                    .MapResult(expr => AssignmentVariableAst.Create(varNameToken.text, expr))
                )
                (stream);
        }

        IParseResult<ReAssignmentVariableAst> ParseReAssignmentVariable(TokenStream stream)
        {
            return
                 ParseVariable()
                .InCompleteErrorWithPositionHead("", ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead("=を期待してます", ErrorCode.Unknown)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
                .Bind(variable =>
                    ParseExpr()
                    .MapResult(expr => ReAssignmentVariableAst.Create(variable.varName, expr))
                    .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
               )
               (stream);
        }

        IParseResult<ReAssignmentIndexerAst> ParseReAssignmentIndexer(TokenStream stream)
        {
            return
                ParserCombinator.Tuple(ParseTerm(), ParseIndexers(true))
                .InCompleteErrorWithPositionHead("", ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead("=を期待してます", ErrorCode.Unknown)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
                .Bind(pair =>
                {
                    if (pair.Item2.Count > 1)
                        pair.Item1 = pair.Item2.Take(pair.Item2.Count - 1)
                        .Aggregate(pair.Item1, (acc, x) => GetIndexerAst.Create(acc, x));

                    return ParseExpr()
                    .MapResult(expr => ReAssignmentIndexerAst.Create(pair.Item1, pair.Item2.Last(), expr))
                    .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError);
                }
               )
               (stream);
        }

        IParseResult<StatementAst> ParseFieldAssignment(TokenStream stream)
        {
            return
                ParseIndexerOpExpr()
                .Bind(instance => ParseDotTerms(instance))
                .InCompleteErrorWithPositionHead("", ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead("=を期待してます", ErrorCode.Unknown)
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
                .Bind<ExprAst, StatementAst>(exprAst =>
                  {
                      if (exprAst is InstanceFieldAst fieldAst)
                          return
                              ParseExpr()
                              .MapResult(expr => FieldAssignmentAst.Create(fieldAst.fieldName, fieldAst.instanceExpr, expr));

                      if (exprAst is GetIndexerAst getIndexerAst)
                          return
                              ParseExpr()
                              .MapResult(expr => ReAssignmentIndexerAst.Create(getIndexerAst.instanceExpr, getIndexerAst.indexExpr, expr));
                      return _ => ParseResult<StatementAst>.CreateError(new Error(ErrorKind.InComplete));
                  })
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

        Parser<VariableAst> ParseVariable()
        {
            return
                ParseToken(TokenType.Id)
                .MapResult(token => VariableAst.Create(token.text));
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
                .Right(ParserCombinator.Separated(ParseVariable(), ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.RightParen))
                (stream);
        }

        IParseResult<VariableAst[]> ParseActionVariableList(TokenStream stream)
        {
            return
                ParseToken(TokenType.PipeOp)
                .Right(ParserCombinator.Separated(ParseVariable(), ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.PipeOp))
                (stream);
        }

        Parser<ArrayLiteralAst> ParseArrayLiteral()
        {
            return
                ParseToken(TokenType.LeftBracket)
                .Right(
                    ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma))
                )
                .Bind<ExprAst[], ArrayLiteralAst>(exprs =>
                     stream =>
                     {
                         var semicolonResult = ParseToken(TokenType.Semicolon)(stream);
                         if (semicolonResult.IsError)
                             return ParseResult<ArrayLiteralAst>.CreateSuccess(
                                 ArrayLiteralAst.Create(exprs, exprs.Length)
                             );
                         var sizeResult = ParseInt(stream);
                         if (sizeResult.IsError)
                             return sizeResult.CastError<ArrayLiteralAst>();
                         return ParseResult<ArrayLiteralAst>.CreateSuccess(
                             ArrayLiteralAst.Create(exprs, (int)sizeResult.Value.value)
                         );
                     }
                )
                .Left(ParseToken(TokenType.RightBracket));
        }

        Parser<ActionAst> ParseActionLiteral()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftCurlyBracket)
                    .Right(ParseActionVariableList),
                    ParseFuncBody(TokenType.RightCurlyBracket)
                    .Left(ParseToken(TokenType.RightCurlyBracket))
                )
                .MapResult(pair =>
                    ActionAst.Create(pair.Item1, pair.Item2)
                );
        }

        Parser<Token> ParseToken(TokenType tokenType)
        {
            return ParserCombinator.TestOnce(token => token.tokenType == tokenType);
        }
    }
}
