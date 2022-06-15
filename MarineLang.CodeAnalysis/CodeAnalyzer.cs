using System.Collections.Generic;
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
        private VariableAst[] _currentStatementVariables;

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

                    var variables = new List<VariableAst>();
                    foreach (var statement in _currentStatements)
                    {
                        if (statement.Range.Contain(position))
                        {
                            _currentStatement = statement;
                            _currentStatementVariables = variables.ToArray();

                            break;
                        }

                        variables.AddRange(statement.LookUp<VariableAst>());
                    }

                    break;
                }
            }
        }
    }
}