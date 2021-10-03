using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.ParserCore;
using MineUtil;
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
        public Parse.Parser<VariableAst> ParseVariable { get; }
        public Parse.Parser<YieldAst> ParseYield { get; }
        public Parse.Parser<BreakAst> ParseBreak { get; }
        public Parse.Parser<ProgramAst> ParseProgram { get; }
        public Parse.Parser<FuncDefinitionAst> ParseFuncDefinition { get; }
        public Parse.Parser<StatementAst> ParseStatement { get; }

        private readonly PluginContainer pluginContainer;

        public SyntaxParser(PluginContainer pluginContainer)
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

        Parse.Parser<ProgramAst> InternalParseProgram()
        {
            return
                Parse.ManyUntilEndStackError(
                   Parse.Or(
                       ParseMacroFuncDefinitions(),
                       ParseFuncDefinition.Map(funcDefinition => new[] { funcDefinition })
                   )
                )
                .Map(funcDefinitions => funcDefinitions.SelectMany(x => x).ToArray())
                .Map(ProgramAst.Create);
        }

        Parse.Parser<FuncDefinitionAst> InternalParseFuncDefinition()
        {
            return input =>
            {
                var headToken = input.Current;
                return
                    ParseToken(TokenType.Func)
                    .NamedError(ErrorCode.SyntaxNonFuncWord, $"\"{input.Current.text}\"")
                    .Right(
                        ParseToken(TokenType.Id)
                         .NamedError(ErrorCode.SyntaxNonFuncName, new RangePosition(headToken.position, headToken.PositionEnd))
                    )
                    .Left(
                        Parse.Except(Parse.End)
                        .NamedError(ErrorCode.SyntaxNonFuncParen)
                    )
                    .Bind(funcNameToken =>
                         ParseVariableList()
                         .NamedError(ErrorCode.SyntaxNonFuncParen, funcNameToken.rangePosition)
                         .Bind(varList =>
                            ParseFuncBody(Parse.Or(ParseToken(TokenType.End), ParseToken(TokenType.Func)))
                            .Map(pair => FuncDefinitionAst.Create(headToken, funcNameToken.text, varList, pair.statementAsts, pair.endToken))
                         )
                     )
                    .Left(
                        Parse.Except(ParseToken(TokenType.Func).NoConsume())
                        .NamedError(ErrorCode.SyntaxNonExpectedFuncWord)
                    )
                    .SwallowIfError(
                        Parse.Many(
                            Parse.Except(
                                Parse.Or(
                                    ParseToken(TokenType.End),
                                    ParseToken(TokenType.Func).NoConsume()
                                )
                            )
                            .Right(Parse.Any) 
                        )
                    )
                    .Left(
                        ParseToken(TokenType.End)
                        .NamedError(ErrorCode.SyntaxNonEndWord)
                    )
                    (input);
            };
        }

        public Parse.Parser<IEnumerable<FuncDefinitionAst>> ParseMacroFuncDefinitions()
        {
            return
                ParseMacro()
                .BindResult(macroBlock =>
                    pluginContainer.GetFuncDefinitionPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parse.Parser<ExprAst> ParseMacroExpr()
        {
            return
                ParseMacro()
                .BindResult(macroBlock =>
                    pluginContainer.GetExprPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parse.Parser<MacroBlock> ParseMacro()
        {
            return
                from macroName in ParseToken(TokenType.MacroName).Left(ParseToken(TokenType.LeftCurlyBracket))
                from tokens in Parse.Until(Parse.Current, ParseToken(TokenType.RightCurlyBracket))
                select new MacroBlock
                {
                    macroName = macroName.text.Substring(1),
                    tokens = tokens
                };
        }

        public Parse.Parser<Block> ParseFuncBody(Parse.Parser<Token> parser)
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
            return
                Parse.OrConsumedError(
                    ParseBreak.Try(),
                    ParseYield.Try(),
                    ParseWhile(),
                    ParseFor(),
                    ParseForEach(),
                    ParseReturn(),
                    ParseAssignmentVariable(),
                    ParseStaticFieldAssignment(),
                    ParseInstanceFieldAssignment(),
                    ParseReAssignmentVariable(),
                    ParseReAssignmentIndexer().Try(),
                    ParseExpr().Map(ExprStatementAst.Create)
                );
        }

        Parse.Parser<YieldAst> InternalParseYield()
        {
            return ParseToken(TokenType.Yield).Map(_ => new YieldAst());
        }

        Parse.Parser<BreakAst> InternalParseBreak()
        {
            return ParseToken(TokenType.Break).Map(_ => new BreakAst());
        }

        public Parse.Parser<WhileAst> ParseWhile()
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

        public Parse.Parser<ForAst> ParseFor()
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

        public Parse.Parser<ForEachAst> ParseForEach()
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
                from elseBlock in elseToken==null?Parse.Return<Block>(null): ParseBlock()
                select
                    IfExprAst.Create(
                        ifToken,
                        conditionExpr,
                        thenBlock.statementAsts,
                        elseBlock?.statementAsts ?? new StatementAst[] { },
                        elseBlock?.endToken ?? thenBlock.endToken
                    );
        }

        public Parse.Parser<Token> ParseBlockEnd()
        {
            return Parse.Or(ParseToken(TokenType.RightCurlyBracket), ParseToken(TokenType.End), ParseToken(TokenType.Func));
        }

        public Parse.Parser<Block> ParseBlock()
        {
            return 
                from _ in ParseToken(TokenType.LeftCurlyBracket)
                from statementAsts in Parse.Until(ParseStatement, ParseBlockEnd().NoConsume())
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
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in opToken == null ? Parse.Return(expr) : ParseBinaryOp2Expr(expr, opToken.tokenType)
                select expr2;
        }

        public Parse.Parser<ExprAst> ParseBinaryOp2Expr(ExprAst beforeExpr, TokenType beforeTokenType)
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
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
                select BinaryOpAst.Create(beforeExpr, expr2, beforeTokenType);
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

        public Parse.Parser<ExprAst> ParseIndexerOpExpr()
        {
            var expr =
                from term in ParseTerm()
                from indexExprs in ParseIndexers(false)
                select indexExprs.Aggregate(term, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
            return Parse.Or(ParseStaticTerm().Try(), expr);
        }

        public Parse.Parser<ExprAst> ParseDotTermExpr(ExprAst instance)
        {
           return from expr in
                Parse.Or<ExprAst>(
                    from awaitToken in ParseToken(TokenType.Await)
                    select AwaitAst.Create(awaitToken, instance)
                    ,
                    from funcCall in ParseFuncCall().Try()
                    select InstanceFuncCallAst.Create(instance, funcCall)
                    ,
                    from variable in ParseVariable
                    select InstanceFieldAst.Create(instance, variable)
                )
            select expr;
        }

        public Parse.Parser<ExprAst> ParseDotTerms(ExprAst instance)
        {
            var dotParser = ParseToken(TokenType.DotOp);

            return input =>
            {
                var parseResult = ParseResult.NewOk(instance, input);

                while (parseResult.Remain.IsEnd == false)
                {
                    var parseResult2 = dotParser(parseResult.Remain);

                    if (parseResult2.Result.IsError)
                        break;

                    parseResult = parseResult.ChainLeft(parseResult2);

                    parseResult = parseResult.ChainRight(ParseDotTermExpr(parseResult.Result.RawValue)(parseResult.Remain));

                    if (parseResult.Remain.IsEnd || parseResult.Result.IsError)
                        break;

                    var parseResult3 = parseResult.ChainRight(ParseIndexers(false)(parseResult.Remain));

                    if (parseResult3.Result.IsError)
                        return parseResult3.CastError<ExprAst>();
                    var instance2 = parseResult3.Result.RawValue.Aggregate(parseResult.Result.RawValue, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));

                    parseResult = parseResult3.Ok(instance2);
                }
                return parseResult;
            };
        }

        public Parse.Parser<Token> ParseBinaryOpToken()
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
                from className in ParseToken(TokenType.ClassName)
                from dotOpToken in ParseToken(TokenType.DotOp)
                select className;

            var funcCallParser =
                from className in classNameParser
                from funcCall in ParseFuncCall()
                select StaticFuncCallAst.Create(className, funcCall);

            var fieldParser =
              from className in classNameParser
              from variable in ParseVariable
              select StaticFieldAst.Create(className, variable);

            return Parse.Or<ExprAst>(funcCallParser.Try(), fieldParser);
        }

        public Parse.Parser<ExprAst> ParseTerm()
        {
            return
                Parse.Or(
                    ParseParenExpr(),
                    ParseTopLevelFuncCall(),
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

        public Parse.Parser<ExprAst> ParseParenExpr()
        {
            return
             ParseToken(TokenType.LeftParen)
             .Right(
                 ParseExpr()
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
                from namespaceTokens in Parse.Many(ParseToken(TokenType.Id).Left(ParseToken(TokenType.TwoColon)).Try())
                from remain in Parse.Remain
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in namespaceTokens.Any()?ParseParamList().Try(): ParseParamList().Try(remain)
                select FuncCallAst.Create(namespaceTokens.ToArray(), funcNameToken, tuple.Item2, tuple.Item3);
        }

        public Parse.Parser<FuncCallAst> ParseFuncCall()
        {
            return
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in ParseParamList().Try()
                select FuncCallAst.Create(funcNameToken, tuple.Item2, tuple.Item3);
        }

        public Parse.Parser<IReadOnlyList<(Token, ExprAst, Token)>> ParseIndexers(bool once)
        {
            var parserIndexer =
                Parse.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParseExpr(),
                    ParseToken(TokenType.RightBracket)
                );

            return once ? Parse.OneMany(parserIndexer) : Parse.Many(parserIndexer);
        }

        public Parse.Parser<ReturnAst> ParseReturn()
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

        public Parse.Parser<AssignmentVariableAst> ParseAssignmentVariable()
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
                    ParseExpr()
                    .NamedError(ErrorCode.SyntaxNonEqualExpr)
                    .Map(expr => AssignmentVariableAst.Create(pair.Item1, pair.Item2, expr))
                );
        }

        public Parse.Parser<ReAssignmentVariableAst> ParseReAssignmentVariable()
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
                    ParseExpr()
                    .Map(expr => ReAssignmentVariableAst.Create(variable, expr))
                    .NamedError(ErrorCode.SyntaxNonEqualExpr)
               );
        }

        public Parse.Parser<ReAssignmentIndexerAst> ParseReAssignmentIndexer()
        {
            return
                (
                    from pair in
                        Parse.Tuple(ParseTerm(), ParseIndexers(true))
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
                    if (pair.Item2.Count > 1)
                        pair.Item1 = pair.Item2.Take(pair.Item2.Count - 1)
                        .Aggregate(pair.Item1, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));

                    return
                        from expr in
                            ParseExpr()
                            .NamedError(ErrorCode.SyntaxNonEqualExpr)
                        select
                            ReAssignmentIndexerAst.Create(
                                GetIndexerAst.Create(pair.Item1, pair.Item2.Last().Item2),
                                expr
                            );
                }
               );
        }

        public Parse.Parser<StatementAst> ParseInstanceFieldAssignment()
        {
            return
                ParseIndexerOpExpr()
                .Bind(ParseDotTerms)
                .Where(expr=>!(expr is VariableAst))
                .NamedError(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .NamedError(ErrorCode.Unknown, "=を期待してます")
                .Left(
                    Parse.Except(Parse.End)
                    .NamedError(ErrorCode.SyntaxNonEqualExpr)
                ).Try()
                .Bind(exprAst =>
                 {
                     if (exprAst is InstanceFieldAst fieldAst)
                         return
                             ParseExpr()
                             .Map(expr =>
                               InstanceFieldAssignmentAst.Create(fieldAst, expr)
                             );

                     if (exprAst is GetIndexerAst getIndexerAst)
                         return
                             ParseExpr()
                             .Map(expr => ReAssignmentIndexerAst.Create(getIndexerAst, expr));

                     return Parse.ErrorReturn<StatementAst>(new ParseErrorInfo("ParseInstanceFieldAssignment"+ exprAst));
                 });
        }

        public Parse.Parser<StaticFieldAssignmentAst> ParseStaticFieldAssignment()
        {
            return
                from className in ParseToken(TokenType.ClassName)
                from dotOp in ParseToken(TokenType.DotOp)
                from variable in ParseVariable
                from equalOp in ParseToken(TokenType.AssignmentOp)
                from expr in ParseExpr()
                select StaticFieldAssignmentAst.Create(StaticFieldAst.Create(className, variable), expr);
        }

        Parse.Parser<ValueAst> InternalParseInt()
        {
            return ParseToken(TokenType.Int)
                .BindResult(token =>
                 (int.TryParse(token.text, out int value)) ?
                     Result.Ok<ValueAst, ParseErrorInfo>(ValueAst.Create(value, token)) :
                     Result.Error<ValueAst, ParseErrorInfo>(new ParseErrorInfo("InternalParseInt"))
             );
        }

        Parse.Parser<ValueAst> InternalParseFloat()
        {
            return ParseToken(TokenType.Float)
               .BindResult(token =>
                    (float.TryParse(token.text, out float value)) ?
                        Result.Ok<ValueAst, ParseErrorInfo>(ValueAst.Create(value, token)) :
                        Result.Error<ValueAst, ParseErrorInfo>(new ParseErrorInfo("InternalParseFloat"))
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

        Parse.Parser<VariableAst> InternalParseVariable()
        {
            return
                ParseToken(TokenType.Id)
                .Map(token => VariableAst.Create(token));
        }

        public Parse.Parser<(Token, ExprAst[], Token)> ParseParamList()
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

        public Parse.Parser<VariableAst[]> ParseVariableList()
        {
            return
                ParseToken(TokenType.LeftParen)
                .Right(Parse.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.RightParen));
        }

        public Parse.Parser<VariableAst[]> ParseActionVariableList()
        {
            return
                ParseToken(TokenType.PipeOp)
                .Right(Parse.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.PipeOp));
        }

        public Parse.Parser<ArrayLiteralAst> ParseArrayLiteral()
        {
            return
                Parse.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    Parse.Separated(ParseExpr(), ParseToken(TokenType.Comma))
                     .Bind<ExprAst[], ArrayLiteralAst.ArrayLiteralExprs, Token>(exprs =>
                       input =>
                       {
                           var semicolonResult = ParseToken(TokenType.Semicolon)(input);
                           if (semicolonResult.Result.IsError)
                               return ParseResult.NewOk(
                                   ArrayLiteralAst.ArrayLiteralExprs.Create(exprs, exprs.Length),
                                   semicolonResult.Remain
                               );

                           var sizeResult = semicolonResult.ChainRight(ParseInt(semicolonResult.Remain));

                           var result =
                              from size in sizeResult.Result
                              select ArrayLiteralAst.ArrayLiteralExprs.Create(exprs, (int)size.value);

                           return sizeResult.SetResult(result);
                       }
                    ),
                    ParseToken(TokenType.RightBracket)
                ).Map(tuple => ArrayLiteralAst.Create(tuple.Item1, tuple.Item2, tuple.Item3));
        }

        public Parse.Parser<ActionAst> ParseActionLiteral()
        {
            return
                Parse.Tuple(
                    ParseToken(TokenType.LeftCurlyBracket),
                    Parse.Or(
                        ParseToken(TokenType.OrOp).Map(_ => new VariableAst[] { }),
                        ParseActionVariableList()
                    ),
                    ParseFuncBody(ParseToken(TokenType.RightCurlyBracket)),
                    ParseToken(TokenType.RightCurlyBracket)
                )
                .Map(pair =>
                    ActionAst.Create(pair.Item1, pair.Item2, pair.Item3.statementAsts, pair.Item4)
                );
        }

        public Parse.Parser<Token> ParseToken(TokenType tokenType)
        {
            return Parse.Expected(token => token.tokenType, tokenType);
        }
    }
}