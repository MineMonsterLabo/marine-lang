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
            public List<Token> tokens;
        }

        //相互依存の無いパーサは使いまわすことで、高速化
        public Parser<ValueAst, Token> ParseInt { get; }
        public Parser<ValueAst, Token> ParseFloat { get; }
        public Parser<ValueAst, Token> ParseBool { get; }
        public Parser<ValueAst, Token> ParseChar { get; }
        public Parser<ValueAst, Token> ParseString { get; }
        public Parser<VariableAst, Token> ParseVariable { get; }
        public Parser<YieldAst, Token> ParseYield { get; }
        public Parser<BreakAst, Token> ParseBreak { get; }
        public Parser<ProgramAst, Token> ParseProgram { get; }
        public Parser<FuncDefinitionAst, Token> ParseFuncDefinition { get; }
        public Parser<StatementAst, Token> ParseStatement { get; }

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

        Parser<ProgramAst, Token> InternalParseProgram()
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

        Parser<FuncDefinitionAst, Token> InternalParseFuncDefinition()
        {
            return input =>
            {
                var headToken = input.Current;
                return
                    ParseToken(TokenType.Func)
                    .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonFuncWord, ErrorKind.ForceError, $"\"{input.Current.text}\"")
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
                    (input);
            };
        }

        public Parser<IEnumerable<FuncDefinitionAst>, Token> ParseMacroFuncDefinitions()
        {
            return
                ParseMacro()
                .BindResult(macroBlock =>
                    pluginContainer.GetFuncDefinitionPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parser<ExprAst, Token> ParseMacroExpr()
        {
            return
                ParseMacro()
                .BindResult(macroBlock =>
                    pluginContainer.GetExprPlugin(macroBlock.macroName).Replace(this, macroBlock.tokens)
                );
        }

        public Parser<MacroBlock, Token> ParseMacro()
        {
            return
                ParseToken(TokenType.MacroName).Left(ParseToken(TokenType.LeftCurlyBracket))
                .Bind<Token, MacroBlock, Token>(macroName =>
                     input =>
                     {
                         var tokens = new List<Token>();
                         while (input.Current.tokenType != TokenType.RightCurlyBracket)
                         {
                             tokens.Add(input.Current);
                             input = input.Advance();
                             if (input.IsEnd)
                                 return ParseResult.Error<MacroBlock, Token>(new ParseErrorInfo(ErrorKind.InComplete), input);
                         }
                         input = input.Advance();
                         return ParseResult.Ok(
                             new MacroBlock
                             {
                                 macroName = macroName.text.Substring(1),
                                 tokens = tokens
                             }, input
                         );
                     }
                );
        }

        public Parser<Block, Token> ParseFuncBody(TokenType endToken)
        {
            return input =>
            {
                var statementAsts = new List<StatementAst>();
                while (input.IsEnd == false && input.Current.tokenType != endToken)
                {
                    var parseResult = ParseStatement(input);
                    input = parseResult.Remain;

                    if (parseResult.Result.IsError)
                        return ParseResult.Error<Block, Token>(parseResult.Result.RawError, input);

                    statementAsts.Add(parseResult.Result.RawValue);
                }

                return ParseResult.Ok(new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = input.LastCurrent
                }, input);
            };
        }

        Parser<StatementAst, Token> InternalParseStatement()
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

        Parser<YieldAst, Token> InternalParseYield()
        {
            return ParseToken(TokenType.Yield).MapResult(_ => new YieldAst());
        }

        Parser<BreakAst, Token> InternalParseBreak()
        {
            return ParseToken(TokenType.Break).MapResult(_ => new BreakAst());
        }

        public Parser<WhileAst, Token> ParseWhile()
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

        public Parser<ForAst, Token> ParseFor()
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

        public Parser<ForEachAst, Token> ParseForEach()
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

        public Parser<ExprAst, Token> ParseExpr()
        {
            return
                ParserCombinator.Or(
                    ParseIfExpr(),
                    ParseMacroExpr(),
                    ParseBinaryOpExpr()
                );
        }

        public Parser<ExprAst, Token> ParseIfExpr()
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

        public Parser<Block, Token> ParseBlock()
        {
            return input =>
            {
                var parseTokenResult = ParseToken(TokenType.LeftCurlyBracket)(input);
                input = parseTokenResult.Remain;

                if (parseTokenResult.Result.IsError || input.IsEnd)
                    return parseTokenResult.Error<Block>(new ParseErrorInfo("", ErrorKind.InComplete));

                var statementAsts = new List<StatementAst>();
                while (input.IsEnd == false && input.Current.tokenType != TokenType.RightCurlyBracket)
                {
                    var parseResult = ParseStatement(input);
                    input = parseResult.Remain;

                    if (parseResult.Result.IsError)
                        return parseResult.Error<Block>(parseResult.Result.RawError);

                    statementAsts.Add(parseResult.Result.RawValue);
                }

                if (input.IsEnd)
                    return ParseResult.Error<Block, Token>(new ParseErrorInfo("", ErrorKind.InComplete), input);

                var endRightCurlyBracketResult = ParseToken(TokenType.RightCurlyBracket)(input);
                input = endRightCurlyBracketResult.Remain;

                if (endRightCurlyBracketResult.Result.IsError)
                    return ParseResult.Error<Block, Token>(new ParseErrorInfo("", ErrorKind.InComplete), input);

                return ParseResult.Ok(new Block
                {
                    statementAsts = statementAsts.ToArray(),
                    endToken = endRightCurlyBracketResult.Result.RawValue
                }, input);
            };
        }

        public Parser<ExprAst, Token> ParseBinaryOpExpr()
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in opToken == null ? Return(expr) : ParseBinaryOp2Expr(expr, opToken.tokenType)
                select expr2;
        }
        public Parser<ExprAst, Token> ParseBinaryOp2Expr(ExprAst beforeExpr, TokenType beforeTokenType)
        {
            return
                from expr in ParseUnaryOpExpr()
                from opToken in ParseBinaryOpToken().Default(null)
                from expr2 in ParseBinaryOp3Expr(beforeExpr, beforeTokenType, expr, opToken)
                select expr2;
        }

        public Parser<ExprAst, Token> ParseBinaryOp3Expr(ExprAst beforeExpr, TokenType beforeTokenType, ExprAst expr, Token opToken)
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

        public Parser<ExprAst, Token> ParseUnaryOpExpr()
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

        public Parser<Token, Token> ParseUnaryOpToken()
        {
            return ParserCombinator.Or(ParseToken(TokenType.MinusOp), ParseToken(TokenType.NotOp));
        }

        public Parser<ExprAst, Token> ParseDotOpExpr()
        {
            return
                from instanceExpr in ParseIndexerOpExpr()
                from expr in ParseDotTerms(instanceExpr)
                select expr;
        }

        public Parser<ExprAst, Token> ParseIndexerOpExpr()
        {
            var expr =
                from term in ParseTerm()
                from indexExprs in ParseIndexers(false)
                select indexExprs.Aggregate(term, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
            return ParserCombinator.Or(ParseStaticTerm().Try(), expr);
        }

        public Parser<ExprAst, Token> ParseDotTerms(ExprAst instance)
        {
            var dotTermParser =
                from dotOpToken in ParseToken(TokenType.DotOp)
                from expr in
                        ParserCombinator.Or<ExprAst, Token>(
                            from awaitToken in ParseToken(TokenType.Await)
                            select AwaitAst.Create(awaitToken, instance),
                            from funcCall in ParseFuncCall().Try()
                            select InstanceFuncCallAst.Create(instance, funcCall),
                            from variable in ParseVariable
                            select InstanceFieldAst.Create(instance, variable)
                        )
                select expr;

            return input =>
            {
                while (input.IsEnd == false)
                {
                    var result = dotTermParser(input);
                    input = result.Remain;

                    if (result.Result.IsError)
                        if (result.Result.RawError.ErrorKind != ErrorKind.InComplete)
                            return result;
                        else break;
                    instance = result.Result.RawValue;
                    if (input.IsEnd)
                        break;

                    var result2 = ParseIndexers(false)(input);
                    input = result2.Remain;

                    if (result2.Result.IsError)
                        return result2.Error<ExprAst>(result2.Result.RawError);
                    instance = result2.Result.RawValue.Aggregate(instance, (acc, x) => GetIndexerAst.Create(acc, x.Item2, x.Item3));
                }
                return ParseResult.Ok(instance, input);
            };
        }

        public Parser<Token, Token> ParseBinaryOpToken()
        {
            return
                ParserCombinator.TestOnce<Token>(token =>
                    token.tokenType >= TokenType.OrOp
                    && token.tokenType <= TokenType.ModOp
                );
        }

        public Parser<ExprAst, Token> ParseStaticTerm()
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

            return ParserCombinator.Or<ExprAst, Token>(funcCallParser.Try(), fieldParser);
        }

        public Parser<ExprAst, Token> ParseTerm()
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

        public Parser<ExprAst, Token> ParseParenExpr()
        {
            return
             ParseToken(TokenType.LeftParen)
             .Right(ParseExpr())
             .Left(ParseToken(TokenType.RightParen));
        }

        public Parser<FuncCallAst, Token> ParseTopLevelFuncCall()
        {
            return
                from namespaceTokens in ParserCombinator.Many(ParseToken(TokenType.Id).Left(ParseToken(TokenType.TwoColon)).Try())
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in ParseParamList().Try()
                select FuncCallAst.Create(namespaceTokens.ToArray(), funcNameToken, tuple.Item2, tuple.Item3);
        }

        public Parser<FuncCallAst, Token> ParseFuncCall()
        {
            return
                from funcNameToken in ParseToken(TokenType.Id)
                from tuple in ParseParamList().Try()
                select FuncCallAst.Create(funcNameToken, tuple.Item2, tuple.Item3);
        }

        public Parser<IReadOnlyList<(Token, ExprAst, Token)>, Token> ParseIndexers(bool once)
        {
            var parserIndexer =
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParseExpr(),
                    ParseToken(TokenType.RightBracket)
                );

            return once ? ParserCombinator.OneMany(parserIndexer) : ParserCombinator.Many(parserIndexer);
        }

        public Parser<ReturnAst, Token> ParseReturn()
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

        public Parser<AssignmentVariableAst, Token> ParseAssignmentVariable()
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

        public Parser<ReAssignmentVariableAst, Token> ParseReAssignmentVariable()
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

        public Parser<ReAssignmentIndexerAst, Token> ParseReAssignmentIndexer()
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

        public Parser<StatementAst, Token> ParseInstanceFieldAssignment()
        {
            return
                ParseIndexerOpExpr()
                .Bind(ParseDotTerms)
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown)
                .Left(ParseToken(TokenType.AssignmentOp))
                .InCompleteErrorWithPositionHead(ErrorCode.Unknown, ErrorKind.None, "=を期待してます")
                .ExpectCanMoveNext()
                .InCompleteErrorWithPositionHead(ErrorCode.SyntaxNonEqualExpr, ErrorKind.ForceError)
                .Bind<ExprAst, StatementAst, Token>(exprAst =>
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
                     return input => ParseResult.Error<StatementAst, Token>(new ParseErrorInfo(ErrorKind.InComplete), input);
                 });
        }

        public Parser<StaticFieldAssignmentAst, Token> ParseStaticFieldAssignment()
        {
            return
                from className in ParseToken(TokenType.ClassName)
                from dotOp in ParseToken(TokenType.DotOp)
                from variable in ParseVariable
                from equalOp in ParseToken(TokenType.AssignmentOp)
                from expr in ParseExpr()
                select StaticFieldAssignmentAst.Create(StaticFieldAst.Create(className, variable), expr);
        }


        Parser<ValueAst, Token> InternalParseInt()
        {
            return ParseToken(TokenType.Int)
                .BindResult(token =>
                 (int.TryParse(token.text, out int value)) ?
                     Result.Ok<ValueAst, ParseErrorInfo>(ValueAst.Create(value, token)) :
                     Result.Error<ValueAst, ParseErrorInfo>(new ParseErrorInfo())
             );
        }

        Parser<ValueAst, Token> InternalParseFloat()
        {
            return ParseToken(TokenType.Float)
               .BindResult(token =>
                    (float.TryParse(token.text, out float value)) ?
                        Result.Ok<ValueAst, ParseErrorInfo>(ValueAst.Create(value, token)) :
                        Result.Error<ValueAst, ParseErrorInfo>(new ParseErrorInfo())
                );
        }

        Parser<ValueAst, Token> InternalParseBool()
        {
            return ParseToken(TokenType.Bool)
                .MapResult(token => ValueAst.Create(bool.Parse(token.text), token));
        }

        Parser<ValueAst, Token> InternalParseChar()
        {
            return ParseToken(TokenType.Char)
              .MapResult(token => ValueAst.Create(token.text[1], token));
        }

        Parser<ValueAst, Token> InternalParseString()
        {
            return ParseToken(TokenType.String)
            .MapResult(token =>
            {
                var text = token.text;
                var value = text.Length == 2 ? "" : text.Substring(1, text.Length - 2);
                return ValueAst.Create(value, token);
            });
        }

        Parser<VariableAst, Token> InternalParseVariable()
        {
            return
                ParseToken(TokenType.Id)
                .MapResult(token => VariableAst.Create(token));
        }

        public Parser<(Token, ExprAst[], Token), Token> ParseParamList()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftParen),
                    ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma)),
                    ParseToken(TokenType.RightParen)
                );
        }

        public Parser<VariableAst[], Token> ParseVariableList()
        {
            return
                ParseToken(TokenType.LeftParen)
                .Right(ParserCombinator.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.RightParen));
        }

        public Parser<VariableAst[], Token> ParseActionVariableList()
        {
            return
                ParseToken(TokenType.PipeOp)
                .Right(ParserCombinator.Separated(ParseVariable, ParseToken(TokenType.Comma)))
                .Left(ParseToken(TokenType.PipeOp));
        }

        public Parser<ArrayLiteralAst, Token> ParseArrayLiteral()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftBracket),
                    ParserCombinator.Separated(ParseExpr(), ParseToken(TokenType.Comma))
                     .Bind<ExprAst[], ArrayLiteralAst.ArrayLiteralExprs, Token>(exprs =>
                      input =>
                      {
                          var semicolonResult = ParseToken(TokenType.Semicolon)(input);
                          input = semicolonResult.Remain;
                          if (semicolonResult.Result.IsError)
                              return semicolonResult.Ok(
                                  ArrayLiteralAst.ArrayLiteralExprs.Create(exprs, exprs.Length)
                              );

                          var sizeResult = ParseInt(input);
                          input = sizeResult.Remain;

                          var result =
                             from size in sizeResult.Result
                             select ArrayLiteralAst.ArrayLiteralExprs.Create(exprs, (int)size.value);

                          return new ParseResult<ArrayLiteralAst.ArrayLiteralExprs, Token>(result, input);
                      }
                    ),
                    ParseToken(TokenType.RightBracket)
                ).MapResult(tuple => ArrayLiteralAst.Create(tuple.Item1, tuple.Item2, tuple.Item3));
        }

        public Parser<ActionAst, Token> ParseActionLiteral()
        {
            return
                ParserCombinator.Tuple(
                    ParseToken(TokenType.LeftCurlyBracket),
                    ParserCombinator.Or(
                        ParseToken(TokenType.OrOp).MapResult(_ => new VariableAst[] { }),
                        ParseActionVariableList()
                    ),
                    ParseFuncBody(TokenType.RightCurlyBracket),
                    ParseToken(TokenType.RightCurlyBracket)
                )
                .MapResult(pair =>
                    ActionAst.Create(pair.Item1, pair.Item2, pair.Item3.statementAsts, pair.Item4)
                );
        }

        public Parser<Token, Token> ParseToken(TokenType tokenType)
        {
            return ParserCombinator.TestOnce<Token>(token =>
            {
                return token.tokenType == tokenType;
            });
        }

        public Parser<T, Token> Return<T>(T t)
        {
            return input => ParseResult.Ok(t, input);
        }
    }
}