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
                var parseResult = ParseStatement()(stream);

                if (parseResult.IsError)
                    return parseResult.CastError<StatementAst[]>();

                statementAsts.Add(parseResult.Value);
            }

            return ParseResult<StatementAst[]>.CreateSuccess(statementAsts.ToArray());
        }

        Parser<StatementAst> ParseStatement()
        {
            return
                ParserCombinator.Or<StatementAst>(
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
                var exprResult = ParseDotOpExpr()(stream);
                if (exprResult.IsError || stream.IsEnd)
                    return exprResult;

                var opResult = ParseOpToken()(stream);
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
                var exprResult = ParseDotOpExpr()(stream);
                if (exprResult.IsError || stream.IsEnd)
                    return exprResult;

                var opResult = ParseOpToken()(stream);
                if (opResult.IsError)
                    return ParseResult<ExprAst>.CreateSuccess(
                        BinaryOpAst.Create(beforeExpr, exprResult.Value, beforeTokenType)
                    );
                var tokenType = opResult.Value.tokenType;
                if (GetOpPriority(beforeTokenType) >= GetOpPriority(tokenType))
                    return
                        ParseBinaryOp2Expr(BinaryOpAst.Create(beforeExpr, exprResult.Value, beforeTokenType), tokenType)(stream);
                return
                    ParseBinaryOp2Expr(exprResult.Value, tokenType)
                    .MapResult(expr => BinaryOpAst.Create(beforeExpr, expr, beforeTokenType))
                    (stream);
            };
        }

        Parser<ExprAst> ParseDotOpExpr()
        {
            return stream =>
            {
                var termResult = ParseIndexerOpExpr(stream);
                if (termResult.IsError || stream.IsEnd)
                    return termResult;

                var fieldResult =
                    ParserCombinator.Many(
                        ParserCombinator.Try(
                            ParserCombinator.Tuple(
                                ParseToken(TokenType.DotOp).Right(
                                    ParserCombinator.Or<ExprAst>(
                                        ParserCombinator.Try(ParseFuncCall),
                                        ParseVariable()
                                    )
                                ),
                                ParseIndexers(false)
                            )
                        )
                    )
                    (stream);
                return
                    fieldResult.Map(funcCallAsts =>
                        funcCallAsts.Aggregate(
                            termResult.Value,
                            (expr, pair) =>
                            {
                                expr = (pair.Item1 is FuncCallAst funcCallAst) ?
                                      InstanceFuncCallAst.Create(expr, funcCallAst) as ExprAst :
                                     InstanceFieldAst.Create(expr, pair.Item1.GetVariableAst().varName);
                                return pair.Item2.Aggregate(expr, (acc, x) => GetIndexerAst.Create(acc, x));
                            }
                        )
                    );
            };
        }

        IParseResult<ExprAst> ParseIndexerOpExpr(TokenStream stream)
        {
            var termResult = ParseTerm()(stream);
            if (termResult.IsError || stream.IsEnd)
                return termResult;

            return ParseIndexers(false)
                .MapResult(indexExprs =>
                    indexExprs.Aggregate(termResult.Value, (acc, x) => GetIndexerAst.Create(acc, x))
                )(stream);
        }

        Parser<Token> ParseOpToken()
        {
            return
                ParserCombinator.TestOnce(token =>
                    token.tokenType >= TokenType.OrOp
                    && token.tokenType <= TokenType.DivOp
                );
        }

        int GetOpPriority(TokenType tokenType)
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

        IParseResult<FieldAssignmentAst> ParseFieldAssignment(TokenStream stream)
        {
            return
                ParserCombinator.Tuple(
                    ParseTerm(),
                    ParserCombinator.OneMany(
                        ParseToken(TokenType.DotOp).Right(ParseVariable())
                    )
                ).MapResult(pair =>
                    pair.Item2.Skip(1).Aggregate(
                        InstanceFieldAst.Create(pair.Item1, pair.Item2[0].varName),
                        (expr, variable) =>
                            InstanceFieldAst.Create(expr, variable.varName)
                    )
                )
                .InCompleteErrorWithPositionHead("", ErrorCode.Unknown)
               .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead("=を期待してます", ErrorCode.Unknown)
                .ExpectCanMoveNext()
                    .InCompleteErrorWithPositionHead("=の後に式がありません", ErrorCode.NonEqualExpr, ErrorKind.ForceError)
               .Bind(fieldAst =>
                    ParseExpr()
                    .MapResult(expr => FieldAssignmentAst.Create(fieldAst.fieldName, fieldAst.instanceExpr, expr))
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

        Parser<Token> ParseToken(TokenType tokenType)
        {
            return ParserCombinator.TestOnce(token => token.tokenType == tokenType);
        }
    }
}
