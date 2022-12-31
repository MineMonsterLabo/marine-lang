using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.ParserCore;
using MarineLang.Utils;
using MineUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    using Parse = Parse<Token>;

    public class SyntaxParser
    {
        public class Block
        {
            public StatementAst[] statementAsts;
            public Token endToken;
        }

        public class MacroBlock
        {
            public string macroName;
            public List<Token> tokens;
        }

        //相互依存の無いパーサは使いまわすことで、高速化
        public Parse.Parser<ValueAst> ParseInt { get; }
        public Parse.Parser<ValueAst> ParseFloat { get; }
        public Parse.Parser<ValueAst> ParseBool { get; }
        public Parse.Parser<ValueAst> ParseChar { get; }
        public Parse.Parser<ValueAst> ParseString { get; }
        public Parse.Parser<ValueAst> ParseNull { get; }
        public Parse.Parser<VariableAst> ParseVariable { get; }
        public Parse.Parser<VariableAst> ParseFieldVariable { get; }
        public Parse.Parser<Token> ParseLowerIdToken { get; }
        public Parse.Parser<Token> ParseUpperIdToken { get; }
        public Parse.Parser<Token> ParseIdToken { get; }
        public Parse.Parser<Token> ParseEndToken { get; }
        public Parse.Parser<Token> ParseFuncToken { get; }
        public Parse.Parser<Token> ParseBinaryOpTokenNullable { get; }
        public Parse.Parser<string> ParseTypeName { get; }
        public Parse.Parser<IEnumerable<string>> ParseTypeParamList { get; }
        public Parse.Parser<StatementAst> ParseBreak { get; }
        public Parse.Parser<ProgramAst> ParseProgram { get; }
        public Parse.Parser<FuncDefinitionAst> ParseFuncDefinition { get; }
        public Parse.Parser<StatementAst> ParseReturn { get; }
        public Parse.Parser<StatementAst> ParseStatement { get; }
        public Parse.Parser<Block> ParseActionFuncBody { get; }
        public Parse.Parser<Block> ParseFuncBody { get; }
        public Parse.Parser<MacroBlock> ParseMacro { get; }
        public Parse.Parser<Token> ParseBlockEndNoConsume { get; }


        private readonly PluginContainer pluginContainer;

        public SyntaxParser(PluginContainer pluginContainer)
        {
            ParseMacro = InternalParseMacro();
            ParseInt = InternalParseInt();
            ParseFloat = InternalParseFloat();
            ParseBool = InternalParseBool();
            ParseChar = InternalParseChar();
            ParseString = InternalParseString();
            ParseNull = InternalParseNull();
            ParseBinaryOpTokenNullable = InternalParseBinaryOpToken().Default(null);
            ParseLowerIdToken = InternalParseLowerIdToken();
            ParseUpperIdToken = InternalParseUpperIdToken();
            ParseIdToken = ParseToken(TokenType.Id);
            ParseEndToken = ParseToken(TokenType.End);
            ParseFuncToken = ParseToken(TokenType.Func);
            ParseBlockEndNoConsume = InternalParseBlockEnd().NoConsume();
            ParseTypeName = InternalParseTypeName();
            ParseTypeParamList = InternalParseTypeParamList();
            ParseVariable = InternalParseVariable();
            ParseFieldVariable = InternalParseFieldVariable();
            ParseBreak = InternalParseBreak().Try().Map(StatementAstExtention.AsStatementAst);
            ParseReturn = InternalParseReturn().Map(StatementAstExtention.AsStatementAst);
            ParseStatement = InternalParseStatement();
            ParseActionFuncBody = InternalParseFuncBody(ParseToken(TokenType.RightCurlyBracket));
            ParseFuncBody = InternalParseFuncBody(Parse.Or(ParseEndToken, ParseFuncToken));
            ParseFuncDefinition = InternalParseFuncDefinition();
            ParseProgram = InternalParseProgram();

            this.pluginContainer = pluginContainer;
        }

        Parse.Parser<ProgramAst> InternalParseProgram()
        {
            return
                Parse.ManyUntilEndStackError(
                   Parse.Or(
                       ParseMacroFuncDefinitions(),
                       ParseFuncDefinition.Map(funcDefinition => new[] { funcDefinition }.AsEnumerable())
                   )
                )
                .Map(funcDefinitions => funcDefinitions.SelectMany(x => x).ToArray())
                .Map(ProgramAst.Create);
        }

        Parse.Parser<FuncDefinitionAst> InternalParseFuncDefinition()
        {
            var endCheckParser =
                Parse.Except(Parse.End)
                .NamedError(ErrorCode.SyntaxNonFuncParen);

            var swallowParser =
                Parse.Many(
                    Parse.Except(
                        Parse.Or(
                            ParseEndToken,
                            ParseFuncToken.NoConsume()
                        )
                    )
                    .Right(Parse.Any)
                );

           var inFuncCheckParser=
                Parse.Except(ParseFuncToken.NoConsume())
                .NamedError(ErrorCode.SyntaxNonExpectedFuncWord);

            var endParser =
                ParseEndToken
                .NamedError(ErrorCode.SyntaxNonEndWord);

            return input =>
            {
                var headToken = input.Current;

                var funcNameParser = 
                    ParseLowerIdToken
                    .NamedError(ErrorCode.SyntaxNonFuncName, new RangePosition(headToken.position, headToken.PositionEnd));

                return
                    ParseFuncToken
                    .NamedError(ErrorCode.SyntaxNonFuncWord, $"\"{input.Current.text}\"")
                    .Right(funcNameParser)
                    .Left(endCheckParser)
                    .Bind(funcNameToken =>
                         ParseVariableList()
                         .NamedError(ErrorCode.SyntaxNonFuncParen, funcNameToken.rangePosition)
                         .Bind(varList =>
                            ParseFuncBody
                            .Map(pair => 
                                FuncDefinitionAst.Create(
                                    headToken, funcNameToken.text, varList.ToArray(), pair.statementAsts, pair.endToken
                                )
                            )
                         )
                     )
                    .Left(inFuncCheckParser)
                    .SwallowIfError(swallowParser)
                    .Left(endParser)
                    (input);
            };
        }

        public Parse.Parser<IEnumerable<FuncDefinitionAst>> ParseMacroFuncDefinitions()
        {
            return
                ParseMacro
                .BindResult((macroBlock, _) =>
                    pluginContainer.GetFuncDefinitionPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parse.Parser<ExprAst> ParseMacroExpr()
        {
            return
                ParseMacro
                .BindResult((macroBlock, _) =>
                    pluginContainer.GetExprPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parse.Parser<MacroBlock> InternalParseMacro()
        {
            return
                from macroName in ParseToken(TokenType.MacroName).Left(ParseToken(TokenType.LeftCurlyBracket))
                from tokens in Parse.Until(Parse.Current, ParseToken(TokenType.RightCurlyBracket))
                select new MacroBlock
                {
                    macroName = macroName.text.Substring(1),
                    tokens = tokens.ToList()
                };
        }

        private Parse.Parser<Block> InternalParseFuncBody(Parse.Parser<Token> parser)
        {
            return
                from statementAsts in Parse.UntilStackError(ParseStatement, parser.NoConsume())
                from endToken in Parse.LastCurrent.NoConsume()
                select new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = endToken
                };
        }

        Parse.Parser<StatementAst> InternalParseStatement()
        {
            var exprParser = ParseExpr();

            return
                Parse.OrConsumedError(
                    ParseBreak,
                    ParseYield(exprParser).Try().Map(StatementAstExtention.AsStatementAst),
                    ParseWhile(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseFor(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseForEach(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseReturn,
                    ParseAssignmentVariable(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseStaticFieldAssignment(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseInstanceFieldAssignment(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseReAssignmentVariable(exprParser).Map(StatementAstExtention.AsStatementAst),
                    ParseReAssignmentIndexer(exprParser).Try().Map(StatementAstExtention.AsStatementAst),
                    exprParser.Map(ExprStatementAst.Create).Map(StatementAstExtention.AsStatementAst)
                );
        }

        Parse.Parser<YieldAst> ParseYield(Parse.Parser<ExprAst> exprParser)
        {
            return
                from yieldToken in
                    ParseToken(TokenType.Yield)
                    .Left(
                        Parse.Except(Parse.End)
                        .NamedError(ErrorCode.SyntaxNonYieldExpr)
                    )
                from expr in exprParser.NamedError(ErrorCode.SyntaxNonYieldExpr)
                select YieldAst.Create(yieldToken, expr);
        }

        Parse.Parser<BreakAst> InternalParseBreak()
        {
            return ParseToken(TokenType.Break).Map(BreakAst.Create);
        }

        public Parse.Parser<WhileAst> ParseWhile(Parse.Parser<ExprAst> exprParser)
        {
            return
                from whileToken in ParseToken(TokenType.While)
                from conditionExpr in exprParser
                from block in ParseBlock()
                select
                    WhileAst.Create(
                        whileToken,
                        conditionExpr,
                        block.statementAsts,
                        block.endToken
                    );
        }

        public Parse.Parser<ForAst> ParseFor(Parse.Parser<ExprAst> exprParser)
        {
            return
                from forToken in ParseToken(TokenType.For)
                from initVariable in ParseVariable.Left(ParseToken(TokenType.AssignmentOp))
                from initExpr in exprParser
                from maxValue in ParseToken(TokenType.Comma).Right(exprParser)
                from addValue in ParseToken(TokenType.Comma).Right(exprParser)
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

        public Parse.Parser<ForEachAst> ParseForEach(Parse.Parser<ExprAst> exprParser)
        {
            return
                from forEachToken in ParseToken(TokenType.ForEach)
                from variable in ParseVariable.Left(ParseToken(TokenType.In))
                from expr in exprParser
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

        public Parse.Parser<ExprAst> ParseExpr()
        {
            return
                Parse.Or(
                    ParseIfExpr(),
                    ParseMacroExpr(),
                    ParseBinaryOpExpr()
                );
        }

        public Parse.Parser<ExprAst> ParseIfExpr()
        {
            return
                from ifToken in ParseToken(TokenType.If)
                from conditionExpr in ParseExpr()
                from thenBlock in ParseBlock()
                from elseToken in ParseToken(TokenType.Else).Default(null)
                from elseBlock in elseToken == null ? Parse.Return<Block>(null) : ParseBlock()
                select
                    IfExprAst.Create(
                        ifToken,
                        conditionExpr,
                        thenBlock.statementAsts,
                        elseBlock?.statementAsts ?? new StatementAst[] { },
                        elseBlock?.endToken ?? thenBlock.endToken
                    ).AsExprAst();
        }

        public Parse.Parser<Token> InternalParseBlockEnd()
        {
            return Parse.Or(ParseToken(TokenType.RightCurlyBracket), ParseEndToken, ParseFuncToken);
        }

        public Parse.Parser<Block> ParseBlock()
        {
            return
                from _ in ParseToken(TokenType.LeftCurlyBracket)
                from statementAsts in Parse.Until(ParseStatement, ParseBlockEndNoConsume)
                from endRightCurlyBracketToken in ParseToken(TokenType.RightCurlyBracket)
                select new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = endRightCurlyBracketToken
                };
        }

        public Parse.Parser<ExprAst> ParseBinaryOpExpr()
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpTokenNullable
                from expr2 in opToken == null ? Parse.Return(expr) : ParseBinaryOp2Expr(expr, opToken.tokenType)
                select expr2;
        }

        public Parse.Parser<ExprAst> ParseBinaryOp2Expr(ExprAst beforeExpr, TokenType beforeTokenType)
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpTokenNullable
                from expr2 in ParseBinaryOp3Expr(beforeExpr, beforeTokenType, expr, opToken)
                select expr2;
        }

        public Parse.Parser<ExprAst> ParseBinaryOp3Expr(ExprAst beforeExpr, TokenType beforeTokenType, ExprAst expr, Token opToken)
        {
            if (opToken == null)
                return
                    Parse.Return<ExprAst>(BinaryOpAst.Create(beforeExpr, expr, beforeTokenType));

            if (
                ExprPriorityHelpr.GetBinaryOpPriority(beforeTokenType)
                >=
                ExprPriorityHelpr.GetBinaryOpPriority(opToken.tokenType)
            )
                return
                    ParseBinaryOp2Expr(BinaryOpAst.Create(beforeExpr, expr, beforeTokenType), opToken.tokenType);
            return
                from expr2 in ParseBinaryOp2Expr(expr, opToken.tokenType)
                select BinaryOpAst.Create(beforeExpr, expr2, beforeTokenType).AsExprAst();
        }

        public Parse.Parser<ExprAst> ParseUnaryOpExpr()
        {
            return
                from opTokens in Parse.Many(ParseUnaryOpToken())
                from expr in ParseDotOpExpr()
                let revOpTokens = opTokens.AsEnumerable().Reverse()
                select
                    revOpTokens.Aggregate(
                        expr,
                        (expr, unaryOpToken) => UnaryOpAst.Create(expr, unaryOpToken)
                    );
        }

        public Parse.Parser<Token> ParseUnaryOpToken()
        {
            return Parse.Or(ParseToken(TokenType.MinusOp), ParseToken(TokenType.NotOp));
        }

        public Parse.Parser<ExprAst> ParseDotOpExpr()
        {
            return
                from instanceExpr in ParseIndexerOpExpr()
                from expr in ParseDotTerms(instanceExpr)
                select expr;
        }

        public Parse.Parser<ExprAst> ParseIndexerOpExpr(Parse.Parser<ExprAst> exprParser = null)
        {
            var expr =
                from term in ParseTerm(exprParser)
                from indexExprs in ParseIndexers(false)
                select indexExprs.Aggregate(term, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
            return Parse.Or(ParseStaticTerm().Try(), expr);
        }

        public Parse.Parser<ExprAst> ParseDotTermExpr(ExprAst instance)
        {
            return from expr in
                 Parse.Or(
                     from awaitToken in ParseToken(TokenType.Await)
                     select AwaitAst.Create(awaitToken, instance).AsExprAst()
                     ,
                     from funcCall in ParseFuncCall().Try()
                     select InstanceFuncCallAst.Create(instance, funcCall).AsExprAst()
                     ,
                     from variable in ParseFieldVariable
                     select InstanceFieldAst.Create(instance, variable).AsExprAst()
                 )
                   select expr;
        }

        public Parse.Parser<ExprAst> ParseDotTerms(ExprAst instance)
        {
            return Parse.FoldIfCheckEnd(ParseToken(TokenType.DotOp).Infinity(), instance,
                (parseResult, dotParser) =>
                {
                    var parseResult2 = dotParser(parseResult.Remain);

                    if (parseResult2.IsError)
                        return (parseResult, false);

                    parseResult = parseResult.ChainLeft(parseResult2);
                    parseResult = parseResult.Bind(v => ParseDotTermExpr(v)(parseResult.Remain));

                    if (parseResult.Remain.IsEnd || parseResult.IsError)
                        return (parseResult, false);

                    var parseResult3 = parseResult.ChainRight(ParseIndexers(false)(parseResult.Remain));

                    if (parseResult3.IsError)
                        return (parseResult3.CastError<ExprAst>(), false);
                    var instance2 = parseResult3.Result.RawValue.Aggregate(parseResult.Result.RawValue, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));

                    return (parseResult3.Ok(instance2), true);
                });
        }

        public Parse.Parser<Token> InternalParseBinaryOpToken()
        {
            return
                Parse.Verify(token =>
                    token.tokenType >= TokenType.OrOp
                    && token.tokenType <= TokenType.ModOp
                );
        }

        public Parse.Parser<ExprAst> ParseStaticTerm()
        {
            var classNameParser =
                from className in ParseUpperIdToken
                from dotOpToken in ParseToken(TokenType.DotOp)
                select className;

            var funcCallParser =
                from className in classNameParser
                from funcCall in ParseFuncCall()
                select StaticFuncCallAst.Create(className, funcCall);

            var fieldParser =
              from className in classNameParser
              from variable in ParseFieldVariable
              select StaticFieldAst.Create(className, variable);

            return Parse.Or(
                funcCallParser.Try().Map(ExprAstExtention.AsExprAst), 
                fieldParser.Map(ExprAstExtention.AsExprAst)
            );
        }

        public Parse.Parser<ExprAst> ParseTerm(Parse.Parser<ExprAst> exprParser = null)
        {
            exprParser = exprParser ?? ParseExpr();

            return
                Parse.Or(
                    ParseParenExpr(exprParser).Map(ExprAstExtention.AsExprAst),
                    ParseTopLevelFuncCall().Map(ExprAstExtention.AsExprAst),
                    ParseFloat.Try().Map(ExprAstExtention.AsExprAst),
                    ParseInt.Try().Map(ExprAstExtention.AsExprAst),
                    ParseBool.Try().Map(ExprAstExtention.AsExprAst),
                    ParseChar.Try().Map(ExprAstExtention.AsExprAst),
                    ParseString.Try().Map(ExprAstExtention.AsExprAst),
                    ParseNull.Map(ExprAstExtention.AsExprAst),
                    ParseArrayLiteral(exprParser).Try().Map(ExprAstExtention.AsExprAst),
                    ParseActionLiteral().Try().Map(ExprAstExtention.AsExprAst),
                    ParseVariable.Try().Map(ExprAstExtention.AsExprAst),
                    ParseDictConsLiteral(exprParser).Try().Map(ExprAstExtention.AsExprAst)
                );
        }

        public Parse.Parser<ExprAst> ParseParenExpr(Parse.Parser<ExprAst> exprParser)
        {
            return
             ParseToken(TokenType.LeftParen)
             .Right(
                 exprParser
                 .SwallowIfError(
                     Parse.Many(
                         Parse.Except(
                             ParseToken(TokenType.RightParen).NoConsume()
                         ).Right(Parse.Any)
                     )
                 )
                 .StackError(ErrorExprAst.Create())
             )
             .Left(ParseToken(TokenType.RightParen));
        }

        public Parse.Parser<FuncCallAst> ParseTopLevelFuncCall()
        {
            return
                from namespaceTokens in Parse.Many(ParseLowerIdToken.Left(ParseToken(TokenType.TwoColon)).Try())
                from remain in Parse.Remain
                from funcNameToken in ParseLowerIdToken
                from tuple in namespaceTokens.Any() ? ParseParamList().Try() : ParseParamList().Try(remain)
                select FuncCallAst.Create(
                    namespaceTokens.ToArray(), 
                    funcNameToken, 
                    new string[] { }, 
                    tuple.Item2.ToArray(), tuple.Item3
                );
        }

        public Parse.Parser<FuncCallAst> ParseFuncCall()
        {
            return
                from funcNameToken in ParseLowerIdToken
                from generitTypeNames in Parse.Optional(ParseTypeParamList)
                from tuple in ParseParamList().Try()
                select FuncCallAst.Create(
                    funcNameToken, 
                    generitTypeNames.UnwrapOr(new string[] { }.AsEnumerable()).ToArray(),
                    tuple.Item2.ToArray(),
                    tuple.Item3
                );
        }

        public Parse.Parser<IEnumerable<(Token, ExprAst, Token)>> ParseIndexers(bool once)
        {
            var parserIndexer =
                Parse.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParseExpr(),
                    ParseToken(TokenType.RightBracket)
                );

            return once ? Parse.OneMany(parserIndexer) : Parse.Many(parserIndexer);
        }

        public Parse.Parser<ReturnAst> InternalParseReturn()
        {
            return
                from pair in Parse.Tuple(
                    ParseToken(TokenType.Return)
                    .NamedError(ErrorCode.Unknown, "retを期待してます")
                    .Left(
                        Parse.Except(Parse.End)
                        .NamedError(ErrorCode.SyntaxNonRetExpr)
                    ),
                    ParseExpr().NamedError(ErrorCode.SyntaxNonRetExpr)
                )
                select ReturnAst.Create(pair.Item1, pair.Item2);
        }

        public Parse.Parser<AssignmentVariableAst> ParseAssignmentVariable(Parse.Parser<ExprAst> exprParser)
        {
            return
                Parse.Tuple(
                    ParseToken(TokenType.Let)
                        .NamedError(ErrorCode.Unknown, "letを期待してます")
                        .Left(
                            Parse.Except(Parse.End)
                            .NamedError(ErrorCode.SyntaxNonLetVarName)
                         ),
                        ParseVariable
                        .NamedError(ErrorCode.SyntaxNonLetVarName)
                        .Left(
                            Parse.Except(Parse.End)
                            .NamedError(ErrorCode.SyntaxNonLetEqual)
                        )
                        .Left(
                            ParseToken(TokenType.AssignmentOp)
                            .NamedError(ErrorCode.SyntaxNonLetEqual)
                        )
                        .Left(
                            Parse.Except(Parse.End)
                            .NamedError(ErrorCode.SyntaxNonEqualExpr)
                        )
                ).Bind(pair =>
                    exprParser.NamedError(ErrorCode.SyntaxNonEqualExpr)
                    .Map(expr => AssignmentVariableAst.Create(pair.Item1, pair.Item2, expr))
                );
        }

        public Parse.Parser<ReAssignmentVariableAst> ParseReAssignmentVariable(Parse.Parser<ExprAst> exprParser)
        {
            return
                 ParseVariable
                .NamedError(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .NamedError(ErrorCode.Unknown, "=を期待してます")
                .Try()
                .Left(
                    Parse.Except(Parse.End)
                    .NamedError(ErrorCode.SyntaxNonEqualExpr)
                )
                .Bind(variable =>
                    exprParser
                    .Map(expr => ReAssignmentVariableAst.Create(variable, expr))
                    .NamedError(ErrorCode.SyntaxNonEqualExpr)
               );
        }

        public Parse.Parser<ReAssignmentIndexerAst> ParseReAssignmentIndexer(Parse.Parser<ExprAst> exprParser)
        {
            return
                (
                    from pair in
                        Parse.Tuple(ParseTerm(exprParser), ParseIndexers(true))
                        .NamedError(ErrorCode.Unknown)
                    from _ in
                        ParseToken(TokenType.AssignmentOp)
                        .NamedError(ErrorCode.Unknown, "=を期待してます")
                        .Left(
                            Parse.Except(Parse.End)
                            .NamedError(ErrorCode.SyntaxNonEqualExpr)
                        )
                    select pair
                )
                .Bind(pair =>
                {
                    var list = pair.Item2.ToArray();
                    if (list.Length > 1)
                        pair.Item1 = pair.Item2.Take(list.Length - 1)
                        .Aggregate(pair.Item1, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));

                    return
                        from expr in exprParser.NamedError(ErrorCode.SyntaxNonEqualExpr)
                        select
                                ReAssignmentIndexerAst.Create(
                                    GetIndexerAst.Create(pair.Item1, pair.Item2.Last().Item2),
                                    expr
                                );
                }
               );
        }

        public Parse.Parser<StatementAst> ParseInstanceFieldAssignment(Parse.Parser<ExprAst> exprParser)
        {
            return
                ParseIndexerOpExpr(exprParser)
                .Bind(ParseDotTerms)
                .Where(expr => !(expr is VariableAst))
                .NamedError(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .NamedError(ErrorCode.Unknown, "=を期待してます")
                .Left(
                    Parse.Except(Parse.End)
                    .NamedError(ErrorCode.SyntaxNonEqualExpr)
                ).Try()
                .Bind((exprAst, input) => exprAst switch
                    {
                        InstanceFieldAst fieldAst =>
                            exprParser
                            .Map(expr =>
                                InstanceFieldAssignmentAst.Create(fieldAst, expr).AsStatementAst()
                            ),

                        GetIndexerAst getIndexerAst =>
                            exprParser
                            .Map(expr => ReAssignmentIndexerAst.Create(getIndexerAst, expr).AsStatementAst()),

                        _ => Parse.ErrorReturn<StatementAst>(new ParseErrorInfo("ParseInstanceFieldAssignment" + exprAst, input.RangePosition))
                    });
        }

        public Parse.Parser<StaticFieldAssignmentAst> ParseStaticFieldAssignment(Parse.Parser<ExprAst> exprParser)
        {
            return
                from remain in Parse.Remain
                from className in ParseUpperIdToken
                from dotOp in ParseToken(TokenType.DotOp)
                from variable in ParseFieldVariable
                from equalOp in ParseToken(TokenType.AssignmentOp).Try(remain)
                from expr in exprParser
                select StaticFieldAssignmentAst.Create(StaticFieldAst.Create(className, variable), expr);
        }

        Parse.Parser<ValueAst> InternalParseInt()
        {
            return ParseToken(TokenType.Int)
                .BindResult((token, input) =>
                 (int.TryParse(token.text, out int value)) ?
                     Result.Ok<ValueAst, IEnumerable<ParseErrorInfo>>(ValueAst.Create(value, token)) :
                     Result.Error<ValueAst, IEnumerable<ParseErrorInfo>>(
                         new[] { new ParseErrorInfo("InternalParseInt", input.RangePosition) }
                     )
             );
        }

        Parse.Parser<ValueAst> InternalParseFloat()
        {
            return ParseToken(TokenType.Float)
               .BindResult((token, input) =>
                    (float.TryParse(token.text, out float value)) ?
                        Result.Ok<ValueAst, IEnumerable<ParseErrorInfo>>(ValueAst.Create(value, token)) :
                        Result.Error<ValueAst, IEnumerable<ParseErrorInfo>>(
                            new[] { new ParseErrorInfo("InternalParseFloat", input.RangePosition) 
                        })
                );
        }

        Parse.Parser<ValueAst> InternalParseBool()
        {
            return ParseToken(TokenType.Bool)
                .Map(token => ValueAst.Create(bool.Parse(token.text), token));
        }

        Parse.Parser<ValueAst> InternalParseChar()
        {
            return ParseToken(TokenType.Char)
              .Map(token => ValueAst.Create(token.text[1], token));
        }

        Parse.Parser<ValueAst> InternalParseString()
        {
            return ParseToken(TokenType.String)
            .Map(token =>
            {
                var text = token.text;
                var value = text.Length == 2 ? "" : text.Substring(1, text.Length - 2);
                return ValueAst.Create(value, token);
            });
        }

        Parse.Parser<ValueAst> InternalParseNull()
        {
            return ParseToken(TokenType.Null)
              .Map(token => ValueAst.Create(null, token));
        }

        Parse.Parser<VariableAst> InternalParseVariable()
        {
            return
                ParseLowerIdToken
                .Map(token => VariableAst.Create(token));
        }

        Parse.Parser<VariableAst> InternalParseFieldVariable()
        {
            return
                ParseIdToken
                .Map(token => VariableAst.Create(token));
        }

        Parse.Parser<IEnumerable<string>> InternalParseTypeParamList()
        {
            return
                 Parse.Tuple(
                     ParseToken(TokenType.LessOp)
                     ,
                     Parse.Separated(ParseTypeName, ParseToken(TokenType.Comma))
                     .SwallowIfError(
                         Parse.Many(
                             Parse.Except(
                                  ParseToken(TokenType.GreaterOp).NoConsume()
                             ).Right(Parse.Any)
                         )
                     ).StackError(new string[] { })
                     ,
                     ParseToken(TokenType.GreaterOp)
                 ).Select(tuple=>tuple.Item2);
        }

        Parse.Parser<string> InternalParseTypeName()
        {
            return
                from tokens in Parse.Separated(ParseIdToken, ParseToken(TokenType.DotOp))
                select string.Join(".", tokens.Select(token => token.text));
        }

        public Parse.Parser<(Token, IEnumerable<ExprAst>, Token)> ParseParamList()
        {
            return
                Parse.Tuple(
                    ParseToken(TokenType.LeftParen)
                    ,
                    Parse.Separated(ParseExpr(), ParseToken(TokenType.Comma))
                    .SwallowIfError(
                        Parse.Many(
                            Parse.Except(
                                 ParseToken(TokenType.RightParen).NoConsume()
                            ).Right(Parse.Any)
                        )
                    ).StackError(new ExprAst[] { })
                    ,
                    ParseToken(TokenType.RightParen)
                );
        }

        public Parse.Parser<IEnumerable<VariableAst>> ParseVariableList()
        {
            return
                ParseToken(TokenType.LeftParen)
                .Right(Parse.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.RightParen));
        }

        public Parse.Parser<IEnumerable<VariableAst>> ParseActionVariableList()
        {
            return
                ParseToken(TokenType.PipeOp)
                .Right(Parse.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.PipeOp));
        }

        public Parse.Parser<ArrayLiteralAst> ParseArrayLiteral(Parse.Parser<ExprAst> exprParser)
        {
            return
                Parse.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    Parse.Separated(exprParser, ParseToken(TokenType.Comma))
                     .Bind<IEnumerable<ExprAst>, ArrayLiteralAst.ArrayLiteralExprs, Token>(exprs2 =>
                       input =>
                       {
                           var exprs = exprs2.ToArray();
                           var semicolonResult = ParseToken(TokenType.Semicolon)(input);
                           if (semicolonResult.IsError)
                               return ParseResult.NewOk(
                                   ArrayLiteralAst.ArrayLiteralExprs.Create(exprs, exprs.Length),
                                   semicolonResult.Remain
                               );

                           var sizeResult = semicolonResult.ChainRight(ParseInt(semicolonResult.Remain));

                           return sizeResult.Map(size => ArrayLiteralAst.ArrayLiteralExprs.Create(exprs, (int)size.value));
                       }
                    ),
                    ParseToken(TokenType.RightBracket)
                ).Map(tuple => ArrayLiteralAst.Create(tuple.Item1, tuple.Item2, tuple.Item3));
        }

        public Parse.Parser<DictionaryConstructAst> ParseDictConsLiteral(Parse.Parser<ExprAst> exprParser)
        {
            var parseDictConsKeyValues =
                 Parse.Separated(ParseDictConsKeyValue(exprParser), ParseToken(TokenType.Comma))
                 .SwallowIfError(
                     Parse.Many(
                         Parse.Except(
                             ParseToken(TokenType.RightCurlyBracket).NoConsume()
                         ).Right(Parse.Any)
                     )
                 ).StackError(new (string id, ExprAst exprAst)[] { });

            return
                from start in ParseToken(TokenType.Dollar)
                from _ in ParseToken(TokenType.LeftCurlyBracket)
                from dictConsKeyValues in parseDictConsKeyValues
                from end in ParseToken(TokenType.RightCurlyBracket)
                select
                    DictionaryConstructAst.Create(
                        start,
                        end,
                        dictConsKeyValues.ToDictionary(item => item.id, item => item.exprAst)
                    );
        }

        public Parse.Parser<(string id, ExprAst exprAst)> ParseDictConsKeyValue(Parse.Parser<ExprAst> exprParser)
        {
            return
                from idToken in ParseLowerIdToken
                from exprAst in ParseToken(TokenType.Colon).Right(exprParser)
                select (id: idToken.text, exprAst: exprAst);
        }

        public Parse.Parser<ActionAst> ParseActionLiteral()
        {
            return
                Parse.Tuple(
                    ParseToken(TokenType.LeftCurlyBracket),
                    Parse.Or(
                        ParseToken(TokenType.OrOp).Map(_ => new VariableAst[] { }.AsEnumerable()),
                        ParseActionVariableList()
                    ),
                    ParseActionFuncBody,
                    ParseToken(TokenType.RightCurlyBracket)
                )
                .Map(pair =>
                    ActionAst.Create(pair.Item1, pair.Item2.ToArray(), pair.Item3.statementAsts, pair.Item4)
                );
        }

        public Parse.Parser<Token> ParseToken(TokenType tokenType)
        {
            return Parse.Expected(token => token.tokenType, tokenType);
        }

        Parse.Parser<Token> InternalParseLowerIdToken()
        {
            return 
                Parse.Verify
                (
                    token => 
                        token.tokenType == TokenType.Id && 
                        CharUtil.IsLowerLetter(token.text[0])
                );
        }

        Parse.Parser<Token> InternalParseUpperIdToken()
        {
            return
                Parse.Verify
                (
                    token =>
                        token.tokenType == TokenType.Id &&
                        CharUtil.IsUpperLetter(token.text[0])
                );
        }
    }
}
