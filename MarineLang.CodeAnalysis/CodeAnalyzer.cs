using System.Collections.Generic;
using System.Linq;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;

namespace MarineLang.CodeAnalysis
{
    public class CodeAnalyzer
    {
        private SyntaxParseResult _result;

        private FuncDefinitionAst[] _funcDefinitions;

        private FuncDefinitionAst _currentFuncDefinition;
        private VariableAst[] _currentFuncParameters;

        private StatementAst[] _currentStatements;

        private StatementAst _currentStatement;
        private ExprAst[] _currentStatementExprs;

        private ExprAst _currentExpr;

        public CodeAnalyzer(SyntaxParseResult result)
        {
            _result = result;
        }

        public void SetResult(SyntaxParseResult result)
        {
            _result = result;
        }

        public void ExecuteAnalyze(Position position)
        {
            var programAst = _result.programAst;

            _funcDefinitions = programAst.funcDefinitionAsts;

            foreach (var funcDefinition in _funcDefinitions)
            {
                if (funcDefinition.Range.Contain(position))
                {
                    _currentFuncDefinition = funcDefinition;
                    _currentFuncParameters = funcDefinition.args;
                    _currentStatements = _currentFuncDefinition.statementAsts;

                    Queue<StatementAst> statementQueue = new Queue<StatementAst>();
                    foreach (StatementAst statement in _currentStatements)
                    {
                        statementQueue.Enqueue(statement);
                    }

                    while (statementQueue.Count > 0)
                    {
                        StatementAst statement = statementQueue.Dequeue();
                        if (statement.Range.Contain(position))
                        {
                            statementQueue.Clear();

                            var children = statement.LookUp<StatementAst>().Except(new[] { statement }).ToArray();
                            foreach (var child in children)
                            {
                                statementQueue.Enqueue(child);
                            }

                            _currentStatement = statement;
                        }
                    }

                    if (_currentStatement != null)
                    {
                        _currentStatementExprs = _currentStatement.LookUp<ExprAst>().ToArray();

                        Queue<ExprAst> exprQueue = new Queue<ExprAst>();
                        foreach (var expr in _currentStatementExprs)
                        {
                            exprQueue.Enqueue(expr);
                        }

                        while (exprQueue.Count > 0)
                        {
                            var exprAst = exprQueue.Dequeue();
                            if (exprAst.Range.Contain(position))
                            {
                                exprQueue.Clear();

                                var children = exprAst.LookUp<ExprAst>().Except(new[] { exprAst }).ToArray();
                                foreach (var child in children)
                                {
                                    exprQueue.Enqueue(child);
                                }

                                _currentExpr = exprAst;
                            }
                        }
                    }

                    break;
                }
            }
        }
    }
}