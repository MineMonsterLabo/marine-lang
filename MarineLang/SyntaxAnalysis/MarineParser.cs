﻿using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MineUtil;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class MarineParser
    {

        public class Block
        {
            public StatementAst[] statementAsts;
            public Token endToken;
        }

        public class MacroBlock
        {
            public string macroName;
            public List<Token>tokens;
        }

        //相互依存の無いパーサは使いまわすことで、高速化
        public Parser<ValueAst> ParseInt { get; }
        public Parser<ValueAst> ParseFloat { get; }
        public Parser<ValueAst> ParseBool { get; }
        public Parser<ValueAst> ParseChar { get; }
        public Parser<ValueAst> ParseString { get; }
        public Parser<VariableAst> ParseVariable { get; }
        public Parser<YieldAst> ParseYield { get; }
        public Parser<BreakAst> ParseBreak { get; }
        public Parser<ProgramAst> ParseProgram { get; }
        public Parser<FuncDefinitionAst> ParseFuncDefinition { get; }
        public Parser<StatementAst> ParseStatement { get; }

        private readonly PluginContainer pluginContainer;

        public MarineParser(PluginContainer pluginContainer)
        {
            ParseInt = InternalParseInt();
            ParseFloat = InternalParseFloat();
            ParseBool = InternalParseBool();
            ParseChar = InternalParseChar();
            ParseString = InternalParseString();
            ParseVariable = InternalParseVariable();
            ParseYield = InternalParseYield();
            ParseBreak = InternalParseBreak();
            ParseStatement = InternalParseStatement();
            ParseFuncDefinition = InternalParseFuncDefinition();
            ParseProgram = InternalParseProgram();

            this.pluginContainer = pluginContainer;
        }

        Parser<ProgramAst> InternalParseProgram()
        {
            return
                ParserCombinator.Many(
                   ParserCombinator.Or(
                       ParseMacroFuncDefinitions(),
                       ParseFuncDefinition.Try().MapResult(funcDefinition => new[] { funcDefinition })
                   )
                )
                .MapResult(funcDefinitions => funcDefinitions.SelectMany(x => x).ToArray())
                .MapResult(ProgramAst.Create);
        }

        Parser<FuncDefinitionAst> InternalParseFuncDefinition()
        {
            return stream =>
            {
                var headToken = stream.Current;
                return
                    ParseToken(TokenType.Func)
                    .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonFuncWord, ErrorKind.ForceError, $"\"{stream.Current.text}\"")
                    .Right(ParseToken(TokenType.Id))
                    .InCompleteError(ErrorCode.SyntaxNonFuncName, new RangePosition(headToken.position, headToken.PositionEnd), ErrorKind.ForceError)
                    .ExpectCanMoveNext()
                    .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonFuncParen, ErrorKind.ForceError)
                    .Bind(funcNameToken =>
                         ParseVariableList().Try()
                         .InCompleteError(ErrorCode.SyntaxNonFuncParen, funcNameToken.rangePosition, ErrorKind.ForceError)
                         .Bind(varList =>
                            ParserExtension.Try(ParseFuncBody(TokenType.End))
                            .MapResult(pair => FuncDefinitionAst.Create(headToken, funcNameToken.text, varList, pair.statementAsts, pair.endToken))
                         )
                     )
                    .Left(ParseToken(TokenType.End))
                    .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonEndWord, ErrorKind.ForceError)
                    (stream);
            };
        }

        public Parser<IEnumerable<FuncDefinitionAst>> ParseMacroFuncDefinitions()
        {
            return
                ParseMacro()
                .BindResult(macroBlock => 
                    pluginContainer.GetFuncDefinitionPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parser<ExprAst> ParseMacroExpr()
        {
            return
                ParseMacro()
                .BindResult(macroBlock =>
                    pluginContainer.GetExprPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parser<MacroBlock> ParseMacro()
        {
            return
                ParseToken(TokenType.MacroName).Left(ParseToken(TokenType.LeftCurlyBracket))
                .Bind<Token, MacroBlock>(macroName =>
                    stream =>
                    {
                        var tokens = new List<Token>();
                        while (stream.Current.tokenType != TokenType.RightCurlyBracket)
                        {
                            tokens.Add(stream.Current);
                            if (stream.MoveNext() == false)
                                return ParseResult.Error<MacroBlock>(new ParseErrorInfo(ErrorKind.InComplete));
                        }
                        stream.MoveNext();
                        return ParseResult.Ok(
                            new MacroBlock
                            {
                                macroName = macroName.text.Substring(1),
                                tokens = tokens
                            }
                        );
                    }
                );
        }

        public Parser<Block> ParseFuncBody(TokenType endToken)
        {
            return stream =>
            {
                var statementAsts = new List<StatementAst>();
                while (stream.IsEnd == false && stream.Current.tokenType != endToken)
                {
                    var parseResult = ParseStatement(stream);

                    if (parseResult.IsError)
                        return ParseResult.Error<Block>(parseResult.RawError);

                    statementAsts.Add(parseResult.RawValue);
                }

                return ParseResult.Ok(new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = stream.LastCurrent
                });
            };
        }

        Parser<StatementAst> InternalParseStatement()
        {
            return
                ParserCombinator.Or(
                    ParseBreak.Try(),
                    ParseYield.Try(),
                    ParseWhile().Try(),
                    ParseFor().Try(),
                    ParseForEach().Try(),
                    ParseReturn().Try(),
                    ParseAssignmentVariable().Try(),
                    ParseStaticFieldAssignment().Try(),
                    ParseInstanceFieldAssignment().Try(),
                    ParseReAssignmentVariable().Try(),
                    ParseReAssignmentIndexer().Try(),
                    ParseExpr().MapResult(ExprStatementAst.Create).Try()
                );
        }

        Parser<YieldAst> InternalParseYield()
        {
            return ParseToken(TokenType.Yield).MapResult(_ => new YieldAst());
        }

        Parser<BreakAst> InternalParseBreak()
        {
            return ParseToken(TokenType.Break).MapResult(_ => new BreakAst());
        }

        public Parser<WhileAst> ParseWhile()
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

        public Parser<ForAst> ParseFor()
        {
            return
                from forToken in ParseToken(TokenType.For)
                from initVariable in ParseVariable.Left(ParseToken(TokenType.AssignmentOp))
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

        public Parser<ForEachAst> ParseForEach()
        {
            return
                from forEachToken in ParseToken(TokenType.ForEach)
                from variable in ParseVariable.Left(ParseToken(TokenType.In))
                from expr in ParseExpr()
                from block in ParseBlock()
                select
                    ForEachAst.Create(
                        forEachToken,
                        variable,
                        expr,
                        block.statementAsts,
                        block.endToken
                    );
        }

        public Parser<ExprAst> ParseExpr()
        {
            return
                ParserCombinator.Or(
                    ParseIfExpr(),
                    ParseMacroExpr(),
                    ParseBinaryOpExpr()
                );
        }

        public Parser<ExprAst> ParseIfExpr()
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

        public Parser<Block> ParseBlock()
        {
            return stream =>
            {
                if (ParseToken(TokenType.LeftCurlyBracket)(stream).IsError || stream.IsEnd)
                    return ParseResult.Error<Block>(new ParseErrorInfo("", ErrorKind.InComplete));

                var statementAsts = new List<StatementAst>();
                while (stream.IsEnd == false && stream.Current.tokenType != TokenType.RightCurlyBracket)
                {
                    var parseResult = ParseStatement(stream);

                    if (parseResult.IsError)
                        return ParseResult.Error<Block>(parseResult.RawError);

                    statementAsts.Add(parseResult.RawValue);
                }

                if (stream.IsEnd)
                    return ParseResult.Error<Block>(new ParseErrorInfo("", ErrorKind.InComplete));

                var endRightCurlyBracketResult = ParseToken(TokenType.RightCurlyBracket)(stream);

                if (endRightCurlyBracketResult.IsError)
                    return ParseResult.Error<Block>(new ParseErrorInfo("", ErrorKind.InComplete));

                return ParseResult.Ok(new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = endRightCurlyBracketResult.RawValue
                });
            };
        }

        public Parser<ExprAst> ParseBinaryOpExpr()
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in opToken == null ? Return(expr) : ParseBinaryOp2Expr(expr, opToken.tokenType)
                select expr2;
        }
        public Parser<ExprAst> ParseBinaryOp2Expr(ExprAst beforeExpr, TokenType beforeTokenType)
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in ParseBinaryOp3Expr(beforeExpr, beforeTokenType, expr, opToken)
                select expr2;
        }

        public Parser<ExprAst> ParseBinaryOp3Expr(ExprAst beforeExpr, TokenType beforeTokenType, ExprAst expr, Token opToken)
        {
            if (opToken == null)
                return
                    Return(BinaryOpAst.Create(beforeExpr, expr, beforeTokenType));

            if (
                ExprPriorityHelpr.GetBinaryOpPriority(beforeTokenType)
                >=
                ExprPriorityHelpr.GetBinaryOpPriority(opToken.tokenType)
            )
                return
                    ParseBinaryOp2Expr(BinaryOpAst.Create(beforeExpr, expr, beforeTokenType), opToken.tokenType);
            return
                from expr2 in ParseBinaryOp2Expr(expr, opToken.tokenType)
                select BinaryOpAst.Create(beforeExpr, expr2, beforeTokenType);
        }

        public Parser<ExprAst> ParseUnaryOpExpr()
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

        public Parser<Token> ParseUnaryOpToken()
        {
            return ParserCombinator.Or(ParseToken(TokenType.MinusOp), ParseToken(TokenType.NotOp));
        }

        public Parser<ExprAst> ParseDotOpExpr()
        {
            return
                from instanceExpr in ParseIndexerOpExpr()
                from expr in ParseDotTerms(instanceExpr)
                select expr;
        }

        public Parser<ExprAst> ParseIndexerOpExpr()
        {
            var expr =
                from term in ParseTerm()
                from indexExprs in ParseIndexers(false)
                select indexExprs.Aggregate(term, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
            return ParserCombinator.Or(ParseStaticTerm().Try(), expr);
        }

        public Parser<ExprAst> ParseDotTerms(ExprAst instance)
        {
            var dotTermParser =
                from dotOpToken in ParseToken(TokenType.DotOp)
                from expr in
                        ParserCombinator.Or<ExprAst>(
                            from awaitToken in ParseToken(TokenType.Await)
                            select AwaitAst.Create(awaitToken, instance),
                            from funcCall in ParseFuncCall().Try()
                            select InstanceFuncCallAst.Create(instance, funcCall),
                            from variable in ParseVariable
                            select InstanceFieldAst.Create(instance, variable)
                        )
                select expr;

            return stream =>
            {
                while (stream.IsEnd == false)
                {
                    var result = dotTermParser(stream);
                    if (result.IsError)
                        if (result.RawError.ErrorKind != ErrorKind.InComplete)
                            return result;
                        else break;
                    instance = result.RawValue;
                    if (stream.IsEnd)
                        break;
                    var result2 = ParseIndexers(false)(stream);
                    if (result2.IsError)
                        return ParseResult.Error<ExprAst>(result2.RawError);
                    instance = result2.RawValue.Aggregate(instance, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
                }
                return ParseResult.Ok(instance);
            };
        }

        public Parser<Token> ParseBinaryOpToken()
        {
            return
                ParserCombinator.TestOnce(token =>
                    token.tokenType >= TokenType.OrOp
                    && token.tokenType <= TokenType.ModOp
                );
        }

        public Parser<ExprAst> ParseStaticTerm()
        {
            var classNameParser =
                from className in ParseToken(TokenType.ClassName)
                from dotOpToken in ParseToken(TokenType.DotOp)
                select className;

            var funcCallParser =
                from className in classNameParser
                from funcCall in ParseFuncCall()
                select StaticFuncCallAst.Create(className,funcCall);

            var fieldParser =
              from className in classNameParser
              from variable in ParseVariable
              select StaticFieldAst.Create(className, variable);

            return ParserCombinator.Or<ExprAst>(funcCallParser.Try(), fieldParser);
        }

        public Parser<ExprAst> ParseTerm()
        {
            return
                ParserCombinator.Or(
                    ParseParenExpr().Try(),
                    ParseTopLevelFuncCall().Try(),
                    ParseFloat.Try(),
                    ParseInt.Try(),
                    ParseBool.Try(),
                    ParseChar.Try(),
                    ParseString.Try(),
                    ParseArrayLiteral().Try(),
                    ParseActionLiteral().Try(),
                    ParseVariable.Try()
                );
        }

        public Parser<ExprAst> ParseParenExpr()
        {
            return
             ParseToken(TokenType.LeftParen)
             .Right(ParseExpr())
             .Left(ParseToken(TokenType.RightParen));
        }

        public Parser<FuncCallAst> ParseTopLevelFuncCall()
        {
            return
                from namespaceTokens in ParserCombinator.Many(ParseToken(TokenType.Id).Left(ParseToken(TokenType.TwoColon)).Try())
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in ParseParamList().Try()
                select FuncCallAst.Create(namespaceTokens.ToArray(), funcNameToken, tuple.Item2, tuple.Item3);
        }

        public Parser<FuncCallAst> ParseFuncCall()
        {
            return
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in ParseParamList().Try()
                select FuncCallAst.Create(funcNameToken, tuple.Item2, tuple.Item3);
        }

        public Parser<IReadOnlyList<(Token, ExprAst, Token)>> ParseIndexers(bool once)
        {
            var parserIndexer =
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParseExpr(),
                    ParseToken(TokenType.RightBracket)
                );

            return once ? ParserCombinator.OneMany(parserIndexer) : ParserCombinator.Many(parserIndexer);
        }

        public Parser<ReturnAst> ParseReturn()
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

        public Parser<AssignmentVariableAst> ParseAssignmentVariable()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.Let)
                        .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "letを期待してます")
                        .ExpectCanMoveNext()
                        .InCompleteErrorWithPositionEnd(ErrorCode.SyntaxNonLetVarName, ErrorKind.ForceError),
                    ParseVariable
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
                    .MapResult(expr => AssignmentVariableAst.Create(pair.Item1, pair.Item2, expr))
                );
        }

        public Parser<ReAssignmentVariableAst> ParseReAssignmentVariable()
        {
            return
                 ParseVariable
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "=を期待してます")
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                .Bind(variable =>
                    ParseExpr()
                    .MapResult(expr => ReAssignmentVariableAst.Create(variable, expr))
                    .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
               );
        }

        public Parser<ReAssignmentIndexerAst> ParseReAssignmentIndexer()
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
                        select
                            ReAssignmentIndexerAst.Create(
                                GetIndexerAst.Create(pair.Item1, pair.Item2.Last().Item2),
                                expr
                            );
                }
               );
        }

        public Parser<StatementAst> ParseInstanceFieldAssignment()
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
                              InstanceFieldAssignmentAst.Create(fieldAst, expr)
                            );

                    if (exprAst is GetIndexerAst getIndexerAst)
                        return
                            ParseExpr()
                            .MapResult(expr => ReAssignmentIndexerAst.Create(getIndexerAst, expr));
                    return _ => ParseResult.Error<StatementAst>(new ParseErrorInfo(ErrorKind.InComplete));
                });
        }

        public Parser<StaticFieldAssignmentAst> ParseStaticFieldAssignment()
        {
            return
                from className in ParseToken(TokenType.ClassName)
                from dotOp in ParseToken(TokenType.DotOp)
                from variable in ParseVariable
                from equalOp in ParseToken(TokenType.AssignmentOp)
                from expr in ParseExpr()
                select StaticFieldAssignmentAst.Create(StaticFieldAst.Create(className, variable), expr);
        }


        Parser<ValueAst> InternalParseInt()
        {
            return ParseToken(TokenType.Int)
                .BindResult(token =>
                 (int.TryParse(token.text, out int value)) ?
                     ParseResult.Ok(ValueAst.Create(value, token)) :
                     ParseResult.Error<ValueAst>(new ParseErrorInfo())
             );
        }

        Parser<ValueAst> InternalParseFloat()
        {
            return ParseToken(TokenType.Float)
               .BindResult(token =>
                    (float.TryParse(token.text, out float value)) ?
                        ParseResult.Ok(ValueAst.Create(value, token)) :
                        ParseResult.Error<ValueAst>(new ParseErrorInfo())
                );
        }

        Parser<ValueAst> InternalParseBool()
        {
            return ParseToken(TokenType.Bool)
                .MapResult(token => ValueAst.Create(bool.Parse(token.text), token));
        }

        Parser<ValueAst> InternalParseChar()
        {
            return ParseToken(TokenType.Char)
              .MapResult(token => ValueAst.Create(token.text[1], token));
        }

        Parser<ValueAst> InternalParseString()
        {
            return ParseToken(TokenType.String)
            .MapResult(token =>
            {
                var text = token.text;
                var value = text.Length == 2 ? "" : text.Substring(1, text.Length - 2);
                return ValueAst.Create(value, token);
            });
        }

        Parser<VariableAst> InternalParseVariable()
        {
            return
                ParseToken(TokenType.Id)
                .MapResult(token => VariableAst.Create(token));
        }

        public Parser<(Token, ExprAst[], Token)> ParseParamList()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftParen),
                    ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma)),
                    ParseToken(TokenType.RightParen)
                );
        }

        public Parser<VariableAst[]> ParseVariableList()
        {
            return
                ParseToken(TokenType.LeftParen)
                .Right(ParserCombinator.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.RightParen));
        }

        public Parser<VariableAst[]> ParseActionVariableList()
        {
            return
                ParseToken(TokenType.PipeOp)
                .Right(ParserCombinator.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.PipeOp));
        }

        public Parser<ArrayLiteralAst> ParseArrayLiteral()
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
                             return ParseResult.Ok(
                                 ArrayLiteralAst.ArrayLiteralExprs.Create( exprs, exprs.Length)
                             );

                         var sizeResult = ParseInt(stream);
                         return
                            from size in sizeResult
                            select  ArrayLiteralAst.ArrayLiteralExprs.Create( exprs, (int)size.value );
                     }
                    ),
                    ParseToken(TokenType.RightBracket)
                ).MapResult(tuple => ArrayLiteralAst.Create(tuple.Item1, tuple.Item2, tuple.Item3));
        }

        public Parser<ActionAst> ParseActionLiteral()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftCurlyBracket),
                    ParserCombinator.Or(
                        ParseToken(TokenType.OrOp).MapResult(_=>new VariableAst[] { }),
                        ParseActionVariableList()
                    ),
                    ParseFuncBody(TokenType.RightCurlyBracket),
                    ParseToken(TokenType.RightCurlyBracket)
                )
                .MapResult(pair =>
                    ActionAst.Create(pair.Item1, pair.Item2, pair.Item3.statementAsts, pair.Item4)
                );
        }

        public Parser<Token> ParseToken(TokenType tokenType)
        {
            return ParserCombinator.TestOnce(token => token.tokenType == tokenType);
        }

        public Parser<T> Return<T>(T t)
        {
            return stream => ParseResult.Ok<T>(t);
        }
    }
}
