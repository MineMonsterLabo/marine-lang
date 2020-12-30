using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.Streams;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class SyntaxAnalyzer
    {
        class Block
        {
            public StatementAst[] statementAsts;
            public Token endToken;
        }

        public IParseResult<ProgramAst> Parse(IEnumerable<Token> tokens)
        {
            var stream = TokenStream.Create(tokens.ToArray());
            stream.MoveNext();

            return
                ParserCombinator.Many(
                    ParserExtension.Try(ParseFuncDefinition)
                )(stream)
                .Map(Enumerable.ToArray)
                .Map(ProgramAst.Create);
        }

        IParseResult<FuncDefinitionAst> ParseFuncDefinition(TokenStream stream)
        {
            var headToken = stream.Current;
            return
                ParseToken(TokenType.Func)
                .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonFuncWord, ErrorKind.None, $"\"{stream.Current.text}\"")
                .Right(ParseToken(TokenType.Id))
                .InCompleteError(ErrorCode.SyntaxNonFuncName, new RangePosition(headToken.position, headToken.PositionEnd))
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonFuncParen)
                .Bind(funcNameToken =>
                     ParserExtension.Try(ParseVariableList)
                     .InCompleteError(ErrorCode.SyntaxNonFuncParen, funcNameToken.rangePosition)
                     .Bind(varList =>
                        ParserExtension.Try(ParseFuncBody(TokenType.End))
                        .MapResult(pair => FuncDefinitionAst.Create(headToken, funcNameToken.text, varList, pair.statementAsts, pair.endToken))
                     )
                 )
                .Left(ParseToken(TokenType.End))
                 .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonEndWord)
                (stream);
        }

        Parser<Block> ParseFuncBody(TokenType endToken)
        {
            return stream =>
            {
                var statementAsts = new List<StatementAst>();
                while (stream.IsEnd == false && stream.Current.tokenType != endToken)
                {
                    var parseResult = ParseStatement()(stream);

                    if (parseResult.IsError)
                        return parseResult.CastError<Block>();

                    statementAsts.Add(parseResult.Value);
                }

                return ParseResult<Block>.CreateSuccess(new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = stream.LastCurrent
                });
            };
        }

        Parser<StatementAst> ParseStatement()
        {
            return
                ParserCombinator.Or(
                    ParserExtension.Try(ParseYield()),
                    ParserExtension.Try(ParseWhile()),
                    ParserExtension.Try(ParseFor()),
                    ParserExtension.Try(ParseReturn()),
                    ParserExtension.Try(ParseAssignmentVariable),
                    ParserExtension.Try(ParseFieldAssignment),
                    ParserExtension.Try(ParseReAssignmentVariable),
                    ParserExtension.Try(ParseReAssignmentIndexer()),
                    ParserExtension.Try(ParseExpr())
                );
        }

        Parser<YieldAst> ParseYield()
        {
            return ParseToken(TokenType.Yield).MapResult(_ => new YieldAst());
        }

        Parser<WhileAst> ParseWhile()
        {
            return
                from whileToken in ParseToken(TokenType.While)
                from conditionExpr in ParseExpr()
                from block in ParseBlock()
                select
                    WhileAst.Create(
                        whileToken,
                        conditionExpr,
                        block.statementAsts,
                        block.endToken
                    );
        }

        Parser<ForAst> ParseFor()
        {
            return
                from forToken in ParseToken(TokenType.For)
                from initVariable in ParseVariable().Left(ParseToken(TokenType.AssignmentOp))
                from initExpr in ParseExpr()
                from maxValue in ParseToken(TokenType.Comma).Right(ParseExpr())
                from addValue in ParseToken(TokenType.Comma).Right(ParseExpr())
                from block in ParseBlock()
                select 
                    ForAst.Create(
                        forToken, 
                        initVariable, 
                        initExpr, 
                        maxValue, 
                        addValue, 
                        block.statementAsts,
                        block.endToken
                    );
        }

        Parser<ExprAst> ParseExpr()
        {
            return  
                ParserCombinator.Or(
                    ParseIfExpr(),
                    ParseBinaryOpExpr()
                );
        }

        Parser<ExprAst> ParseIfExpr()
        {
            return
                from ifToken in ParseToken(TokenType.If)
                from conditionExpr in ParseExpr()
                from thenBlock in ParseBlock()
                from elseBlock in ParseToken(TokenType.Else).Right(ParseBlock()).Try().Default(null)
                select
                           IfExprAst.Create(
                               ifToken,
                               conditionExpr,
                               thenBlock.statementAsts,
                               elseBlock?.statementAsts ?? new StatementAst[] { },
                               elseBlock?.endToken ?? thenBlock.endToken
                           );
        }

        Parser<Block> ParseBlock()
        {
            return stream =>
            {
                if (ParseToken(TokenType.LeftCurlyBracket)(stream).IsError || stream.IsEnd)
                    return ParseResult<Block>.CreateError(new ParseErrorInfo("", ErrorKind.InComplete));

                var statementAsts = new List<StatementAst>();
                while (stream.IsEnd == false && stream.Current.tokenType != TokenType.RightCurlyBracket)
                {
                    var parseResult = ParseStatement()(stream);

                    if (parseResult.IsError)
                        return parseResult.CastError<Block>();

                    statementAsts.Add(parseResult.Value);
                }

                if (stream.IsEnd)
                    return ParseResult<Block>.CreateError(new ParseErrorInfo("", ErrorKind.InComplete));

                var endRightCurlyBracketResult = ParseToken(TokenType.RightCurlyBracket)(stream);

                if (endRightCurlyBracketResult.IsError)
                    return ParseResult<Block>.CreateError(new ParseErrorInfo("", ErrorKind.InComplete));

                return ParseResult<Block>.CreateSuccess(new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = endRightCurlyBracketResult.Value
                });
            };
        }

        Parser<ExprAst> ParseBinaryOpExpr()
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in opToken == null ? Return(expr) : ParseBinaryOp2Expr(expr, opToken.tokenType)
                select expr2;
        }
        Parser<ExprAst> ParseBinaryOp2Expr(ExprAst beforeExpr, TokenType beforeTokenType)
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in ParseBinaryOp3Expr(beforeExpr, beforeTokenType, expr, opToken)
                select expr2;
        }

        Parser<ExprAst> ParseBinaryOp3Expr(ExprAst beforeExpr, TokenType beforeTokenType, ExprAst expr, Token opToken)
        {
            if (opToken == null)
                return 
                    Return(BinaryOpAst.Create(beforeExpr, expr, beforeTokenType));
         
            if (GetBinaryOpPriority(beforeTokenType) >= GetBinaryOpPriority(opToken.tokenType))
                return
                    ParseBinaryOp2Expr(BinaryOpAst.Create(beforeExpr, expr, beforeTokenType), opToken.tokenType);
            return
                from expr2 in ParseBinaryOp2Expr(expr, opToken.tokenType)
                select BinaryOpAst.Create(beforeExpr, expr2, beforeTokenType);
        }

        Parser<ExprAst> ParseUnaryOpExpr()
        {
            return
                from opTokens in ParserCombinator.Many(ParseUnaryOpToken())
                from expr in ParseDotOpExpr()
                let revOpTokens = opTokens.AsEnumerable().Reverse()
                select 
                    revOpTokens.Aggregate(
                        expr, 
                        (expr, unaryOpToken) => UnaryOpAst.Create(expr, unaryOpToken)
                    );
        }

        Parser<Token> ParseUnaryOpToken()
        {
            return ParserCombinator.Or(ParseToken(TokenType.MinusOp), ParseToken(TokenType.NotOp));
        }

        Parser<ExprAst> ParseDotOpExpr()
        {
            return
                from instanceExpr in ParseIndexerOpExpr()
                from expr in ParseDotTerms(instanceExpr)
                select expr;
        }

        Parser<ExprAst> ParseIndexerOpExpr()
        {
            return
                from term in ParseTerm()
                from indexExprs in ParseIndexers(false)
                select indexExprs.Aggregate(term, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
        }

        Parser<ExprAst> ParseDotTerms(ExprAst instance)
        {
            var dotTermParser =
                from dotOpToken in ParseToken(TokenType.DotOp)
                from expr in
                        ParserCombinator.Or<ExprAst>(
                            from awaitToken in ParseToken(TokenType.Await)
                            select AwaitAst.Create(awaitToken, instance),
                            from funcCall in ParseFuncCall().Try()
                            select InstanceFuncCallAst.Create(instance, funcCall),
                            from variable in ParseVariable()
                            select InstanceFieldAst.Create(instance, variable)
                        )
                select expr;

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
                    instance = result2.Value.Aggregate(instance, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
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
                    ParserExtension.Try(ParseParenExpr()),
                    ParserExtension.Try(ParseFuncCall()),
                    ParserExtension.Try(ParseFloat),
                    ParserExtension.Try(ParseInt),
                    ParserExtension.Try(ParseBool),
                    ParserExtension.Try(ParseChar),
                    ParserExtension.Try(ParseString),
                    ParserExtension.Try(ParseArrayLiteral()),
                    ParserExtension.Try(ParseActionLiteral()),
                    ParserExtension.Try(ParseVariable())
                );
        }

        Parser<ExprAst> ParseParenExpr()
        {
            return
             ParseToken(TokenType.LeftParen)
             .Right(ParseExpr())
             .Left(ParseToken(TokenType.RightParen));
        }


        Parser<FuncCallAst> ParseFuncCall()
        {
            return
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in ParserExtension.Try(ParseParamList)
                select FuncCallAst.Create(funcNameToken, tuple.Item2, tuple.Item3);
        }

        Parser<IReadOnlyList<(Token, ExprAst, Token)>> ParseIndexers(bool once)
        {
            var parserIndexer =
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParseExpr(),
                    ParseToken(TokenType.RightBracket)
                );

            return once ? ParserCombinator.OneMany(parserIndexer) : ParserCombinator.Many(parserIndexer);
        }

        Parser<ReturnAst> ParseReturn()
        {
            return
                from pair in ParserCombinator.Tuple(
                    ParseToken(TokenType.Return)
                    .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "retを期待してます")
                    .ExpectCanMoveNext()
                    .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonRetExpr, ErrorKind.ForceError),
                    ParseExpr().InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonRetExpr, ErrorKind.ForceError)
                )
                select ReturnAst.Create(pair.Item1, pair.Item2);
        }

        IParseResult<AssignmentVariableAst> ParseAssignmentVariable
            (TokenStream stream)
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.Let)
                        .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "letを期待してます")
                        .ExpectCanMoveNext()
                        .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonLetVarName, ErrorKind.ForceError),
                    ParseToken(TokenType.Id)
                        .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonLetVarName, ErrorKind.ForceError)
                        .ExpectCanMoveNext()
                        .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonLetEqual, ErrorKind.ForceError)
                        .Left(
                            ParseToken(TokenType.AssignmentOp)
                            .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonLetEqual, ErrorKind.ForceError)
                        )
                        .ExpectCanMoveNext()
                        .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                ).Bind(pair =>
                    ParseExpr()
                    .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                    .MapResult(expr => AssignmentVariableAst.Create(pair.Item1, pair.Item2.text, expr))
                )
                (stream);
        }

        IParseResult<ReAssignmentVariableAst> ParseReAssignmentVariable(TokenStream stream)
        {
            return
                 ParseVariable()
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "=を期待してます")
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                .Bind(variable =>
                    ParseExpr()
                    .MapResult(expr => ReAssignmentVariableAst.Create(variable.varToken, expr))
                    .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
               )
               (stream);
        }

        Parser<ReAssignmentIndexerAst> ParseReAssignmentIndexer()
        {
            return
                (
                    from pair in
                        ParserCombinator.Tuple(ParseTerm(), ParseIndexers(true))
                        .InCompleteErrorWithPositionHead(ErrorCode.Unknown)
                    from _ in
                        ParseToken(TokenType.AssignmentOp)
                        .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "=を期待してます")
                        .ExpectCanMoveNext()
                        .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                    select pair
                )
                .Bind(pair =>
                {
                    if (pair.Item2.Count > 1)
                        pair.Item1 = pair.Item2.Take(pair.Item2.Count - 1)
                        .Aggregate(pair.Item1, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));

                    return 
                        from expr in 
                            ParseExpr()
                            .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                        select ReAssignmentIndexerAst.Create(pair.Item1, pair.Item2.Last().Item2, expr);
                }
               );
        }

        IParseResult<StatementAst> ParseFieldAssignment(TokenStream stream)
        {
            return
                ParseIndexerOpExpr()
                .Bind(ParseDotTerms)
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "=を期待してます")
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                .Bind<ExprAst, StatementAst>(exprAst =>
                  {
                      if (exprAst is InstanceFieldAst fieldAst)
                          return
                              ParseExpr()
                              .MapResult(expr =>
                                FieldAssignmentAst.Create(fieldAst.variableAst, fieldAst.instanceExpr, expr)
                              );

                      if (exprAst is GetIndexerAst getIndexerAst)
                          return
                              ParseExpr()
                              .MapResult(expr => ReAssignmentIndexerAst.Create(getIndexerAst.instanceExpr, getIndexerAst.indexExpr, expr));
                      return _ => ParseResult<StatementAst>.CreateError(new ParseErrorInfo(ErrorKind.InComplete));
                  })
                (stream);
        }

        IParseResult<ValueAst> ParseInt(TokenStream stream)
        {
            return ParseToken(TokenType.Int)
                .BindResult(token =>
                 (int.TryParse(token.text, out int value)) ?
                     ParseResult<ValueAst>.CreateSuccess(ValueAst.Create(value, token)) :
                     ParseResult<ValueAst>.CreateError(new ParseErrorInfo())
             )(stream);
        }

        IParseResult<ValueAst> ParseFloat(TokenStream stream)
        {
            return ParseToken(TokenType.Float)
               .BindResult(token =>
                    (float.TryParse(token.text, out float value)) ?
                        ParseResult<ValueAst>.CreateSuccess(ValueAst.Create(value, token)) :
                        ParseResult<ValueAst>.CreateError(new ParseErrorInfo())
                )(stream);
        }

        IParseResult<ValueAst> ParseBool(TokenStream stream)
        {
            return ParseToken(TokenType.Bool)
                .MapResult(token => ValueAst.Create(bool.Parse(token.text), token))
                (stream);
        }

        IParseResult<ValueAst> ParseChar(TokenStream stream)
        {
            return ParseToken(TokenType.Char)
              .MapResult(token => ValueAst.Create(token.text[1], token))
              (stream);
        }

        IParseResult<ValueAst> ParseString(TokenStream stream)
        {
            return ParseToken(TokenType.String)
            .MapResult(token =>
            {
                var text = token.text;
                var value = text.Length == 2 ? "" : text.Substring(1, text.Length - 2);
                return ValueAst.Create(value, token);
            })
            (stream);
        }

        Parser<VariableAst> ParseVariable()
        {
            return
                ParseToken(TokenType.Id)
                .MapResult(token => VariableAst.Create(token));
        }

        IParseResult<(Token, ExprAst[], Token)> ParseParamList(TokenStream stream)
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftParen),
                    ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma)),
                    ParseToken(TokenType.RightParen)
                )(stream);
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
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma))
                     .Bind<ExprAst[], ArrayLiteralAst.ArrayLiteralExprs>(exprs =>
                     stream =>
                     {
                         var semicolonResult = ParseToken(TokenType.Semicolon)(stream);
                         if (semicolonResult.IsError)
                             return ParseResult<ArrayLiteralAst.ArrayLiteralExprs>.CreateSuccess(
                                new ArrayLiteralAst.ArrayLiteralExprs { exprAsts = exprs, size = exprs.Length }
                             );
                         var sizeResult = ParseInt(stream);
                         if (sizeResult.IsError)
                             return sizeResult.CastError<ArrayLiteralAst.ArrayLiteralExprs>();
                         return ParseResult<ArrayLiteralAst.ArrayLiteralExprs>.CreateSuccess(
                             new ArrayLiteralAst.ArrayLiteralExprs { exprAsts = exprs, size = (int)sizeResult.Value.value }
                         );
                     }
                    ),
                    ParseToken(TokenType.RightBracket)
                ).MapResult(tuple => ArrayLiteralAst.Create(tuple.Item1, tuple.Item2, tuple.Item3));
        }

        Parser<ActionAst> ParseActionLiteral()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftCurlyBracket),
                    ParseActionVariableList,
                    ParseFuncBody(TokenType.RightCurlyBracket),
                    ParseToken(TokenType.RightCurlyBracket)
                )
                .MapResult(pair =>
                    ActionAst.Create(pair.Item1, pair.Item2, pair.Item3.statementAsts, pair.Item4)
                );
        }

        Parser<Token> ParseToken(TokenType tokenType)
        {
            return ParserCombinator.TestOnce(token => token.tokenType == tokenType);
        }

        Parser<T> Return<T>(T t)
        {
            return stream => ParseResult<T>.CreateSuccess(t);
        }
    }
}
