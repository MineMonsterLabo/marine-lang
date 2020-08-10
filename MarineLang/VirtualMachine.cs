﻿using MarineLang.BuiltInTypes;
using MarineLang.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang
{
    public class VirtualMachine
    {

        Dictionary<string, MethodInfo> methodInfoDict = new Dictionary<string, MethodInfo>();
        Dictionary<string, FuncDefinitionAst> marineFuncDict;
        Stack<Dictionary<string, object>> variables = new Stack<Dictionary<string, object>>();

        public void Register(MethodInfo methodInfo)
        {
            methodInfoDict.Add(methodInfo.Name, methodInfo);
        }

        public void SetProgram(ProgramAst programAst)
        {
            variables.Clear();
            marineFuncDict = programAst.funcDefinitionAsts.ToDictionary(v => v.funcName);
        }
        public RET Run<RET>(string marineFuncName)
        {
            return (RET)RunFuncDefinitionAst(marineFuncDict[marineFuncName]);
        }

        object RunFuncCallAst(FuncCallAst funcCallAst)
        {
            if (marineFuncDict.ContainsKey(funcCallAst.funcName))
                return RunFuncDefinitionAst(marineFuncDict[funcCallAst.funcName]);
            else
                return
                    methodInfoDict[funcCallAst.funcName]
                    .Invoke(null, funcCallAst.args.Select(expr => RunExpr(expr)).ToArray());
        }

        object RunFuncDefinitionAst(FuncDefinitionAst funcDefinitionAst)
        {
            variables.Push(new Dictionary<string, object>());
            foreach (var statementAst in funcDefinitionAst.statementAsts)
            {
                if (statementAst.GetExprAst() != null)
                    RunExpr(statementAst.GetExprAst());
                else if (statementAst.GetAssignmentAst() != null)
                {
                    var assignment = statementAst.GetAssignmentAst();
                    variables.Peek()[assignment.varName] = RunExpr(assignment.expr);
                }
                else if (statementAst.GetReAssignmentAst() != null)
                {
                    var assignment = statementAst.GetReAssignmentAst();
                    variables.Peek()[assignment.varName] = RunExpr(assignment.expr);
                }
                else if (statementAst.GetReturnAst() != null)
                {
                    var ret = RunExpr(statementAst.GetReturnAst().expr);
                    variables.Pop();
                    return ret;
                }
            }
            variables.Pop();
            return new UnitType();
        }

        object RunExpr(ExprAst exprAst)
        {
            if (exprAst.GetFuncCallAst() != null)
                return RunFuncCallAst(exprAst.GetFuncCallAst());
            else if (exprAst.GetValueAst() != null)
                return exprAst.GetValueAst().value;
            else if (exprAst.GetVariableAst() != null)
                return variables.Peek()[exprAst.GetVariableAst().varName];
            return null;
        }
    }
}
