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
            return (RET)RunFuncDefinitionAst(marineFuncDict[marineFuncName], marineFuncDict);
        }

        void RunFuncCallAst(FuncCallAst funcCallAst, Dictionary<string, FuncDefinitionAst> marineFuncDict)
        {
            if (marineFuncDict.ContainsKey(funcCallAst.funcName))
                RunFuncDefinitionAst(marineFuncDict[funcCallAst.funcName], marineFuncDict);
            else
                methodInfoDict[funcCallAst.funcName].Invoke(null, new object[] { });
        }

        object RunFuncDefinitionAst(FuncDefinitionAst funcDefinitionAst, Dictionary<string, FuncDefinitionAst> marineFuncDict)
        {
            foreach (var statementAst in funcDefinitionAst.statementAsts)
            {
                if (statementAst.GetFuncCallAst() != null)
                    RunFuncCallAst(statementAst.GetFuncCallAst(), marineFuncDict);
                else if (statementAst.GetReturnAst() != null)
                    return statementAst.GetReturnAst().value;
            }
            return new UnitType();
        }
    }
}
