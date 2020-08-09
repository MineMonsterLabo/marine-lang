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
        public void Run(string marineFuncName)
        {
            RunFuncDefinitionAst(marineFuncDict[marineFuncName], marineFuncDict);
        }

        void RunFuncCallAst(FuncCallAst funcCallAst, Dictionary<string, FuncDefinitionAst> marineFuncDict)
        {
            if (marineFuncDict.ContainsKey(funcCallAst.funcName))
                RunFuncDefinitionAst(marineFuncDict[funcCallAst.funcName], marineFuncDict);
            else
                methodInfoDict[funcCallAst.funcName].Invoke(null, new object[] { });
        }

        void RunFuncDefinitionAst(FuncDefinitionAst funcDefinitionAst, Dictionary<string, FuncDefinitionAst> marineFuncDict)
        {
            foreach (var funcCallAst in funcDefinitionAst.statementAsts)
                RunFuncCallAst(funcCallAst, marineFuncDict);
        }
    }
}
