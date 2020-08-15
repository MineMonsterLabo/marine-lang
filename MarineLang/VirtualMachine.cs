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
        public RET Run<RET>(string marineFuncName, params object[] args)
        {
            return (RET)RunFuncDefinitionAst(marineFuncDict[marineFuncName], args);
        }

        object RunFuncCallAst(FuncCallAst funcCallAst)
        {
            var args = funcCallAst.args.Select(expr => RunExpr(expr)).ToArray();

            if (marineFuncDict.ContainsKey(funcCallAst.funcName))
                return RunFuncDefinitionAst(marineFuncDict[funcCallAst.funcName], args);
            else
                return
                    methodInfoDict[funcCallAst.funcName]
                    .Invoke(null, args);
        }

        object RunFuncDefinitionAst(FuncDefinitionAst funcDefinitionAst, object[] args)
        {
            variables.Push(new Dictionary<string, object>());

            for (var i = 0; i < funcDefinitionAst.args.Length; i++)
            {
                var variable = funcDefinitionAst.args[i];
                variables.Peek()[variable.varName] = args[i];
            }

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
            else if (exprAst.GetBinaryOpAst() != null)
                return RunBinaryOp(exprAst.GetBinaryOpAst());

            return null;
        }

        object RunBinaryOp(BinaryOpAst binaryOpAst)
        {
            var leftValue = RunExpr(binaryOpAst.leftExpr);
            var rightValue = RunExpr(binaryOpAst.rightExpr);

            switch (binaryOpAst.opKind)
            {
                case TokenType.PlusOp:
                    switch (leftValue)
                    {
                        case int v: return v + (int)rightValue;
                        case float v: return v + (float)rightValue;
                        case string v: return v + rightValue;
                    }
                    break;
                case TokenType.MinusOp:
                    switch (leftValue)
                    {
                        case int v: return v - (int)rightValue;
                        case float v: return v - (float)rightValue;
                    }
                    break;
                case TokenType.MulOp:
                    switch (leftValue)
                    {
                        case int v: return v * (int)rightValue;
                        case float v: return v * (float)rightValue;
                    }
                    break;
                case TokenType.DivOp:
                    switch (leftValue)
                    {
                        case int v: return v / (int)rightValue;
                        case float v: return v / (float)rightValue;
                    }
                    break;
                case TokenType.EqualOp:
                    return leftValue.Equals(rightValue);
                case TokenType.NotEqualOp:
                    return !leftValue.Equals(rightValue);
                case TokenType.OrOp:
                    return (bool)leftValue || (bool)rightValue;
                case TokenType.AndOp:
                    return (bool)leftValue && (bool)rightValue;
                case TokenType.GreaterOp:
                    switch (leftValue)
                    {
                        case int v: return v > (int)rightValue;
                        case float v: return v > (float)rightValue;
                    }
                    break;
                case TokenType.GreaterEqualOp:
                    switch (leftValue)
                    {
                        case int v: return v >= (int)rightValue;
                        case float v: return v >= (float)rightValue;
                    }
                    break;

                case TokenType.LessOp:
                    switch (leftValue)
                    {
                        case int v: return v < (int)rightValue;
                        case float v: return v < (float)rightValue;
                    }
                    break;

                case TokenType.LessEqualOp:
                    switch (leftValue)
                    {
                        case int v: return v <= (int)rightValue;
                        case float v: return v <= (float)rightValue;
                    }
                    break;
            }
            return null;
        }
    }

}