using MarineLang.BuiltInTypes;
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

        public void Register(MethodInfo methodInfo)
        {
            methodInfoDict.Add(methodInfo.Name, methodInfo);
        }

        public void SetProgram(ProgramAst programAst)
        {
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
                return methodInfoDict[funcCallAst.funcName].Invoke(null, new object[] { });
        }

        object RunFuncDefinitionAst(FuncDefinitionAst funcDefinitionAst)
        {
            foreach (var statementAst in funcDefinitionAst.statementAsts)
            {
                if (statementAst.GetExprAst() != null)
                    RunExpr(statementAst.GetExprAst());
                else if (statementAst.GetReturnAst() != null)
                    return RunExpr(statementAst.GetReturnAst().expr);
            }
            return new UnitType();
        }

        object RunExpr(ExprAst exprAst)
        {
            if (exprAst.GetFuncCallAst() != null)
                return RunFuncCallAst(exprAst.GetFuncCallAst());
            else if (exprAst.GetValueAst<int>() != null)
                return exprAst.GetValueAst<int>().value;
            return null;
        }
    }
}
