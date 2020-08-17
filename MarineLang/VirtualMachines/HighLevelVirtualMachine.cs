using MarineLang.Models;
using MarineLang.VirtualMachines;
using System.Collections.Generic;
using System.Reflection;

namespace MarineLang
{
    public class HighLevelVirtualMachine
    {
        Dictionary<string, MethodInfo> methodInfoDict = new Dictionary<string, MethodInfo>();
        ILGeneratedData iLGeneratedData;
        LowLevelVirtualMachine lowLevelVirtualMachine = new LowLevelVirtualMachine();
        ILGenerator iLGenerator;

        public IReadOnlyList<IMarineIL> MarineILs => iLGeneratedData?.marineILs;

        public void Register(MethodInfo methodInfo)
        {
            methodInfoDict.Add(methodInfo.Name, methodInfo);
        }

        public void SetProgram(ProgramAst programAst)
        {
            iLGenerator = new ILGenerator(programAst);
        }

        public void Compile()
        {
            iLGeneratedData = iLGenerator.Generate(methodInfoDict);
        }

        public RET Run<RET>(string marineFuncName, params object[] args)
        {
            lowLevelVirtualMachine.Init();
            lowLevelVirtualMachine.nextILIndex = iLGeneratedData.funcILIndexDict[marineFuncName];
            foreach (var arg in args)
                lowLevelVirtualMachine.Push(arg);
            lowLevelVirtualMachine.Push(lowLevelVirtualMachine.stackBaseCount);
            lowLevelVirtualMachine.Push(lowLevelVirtualMachine.nextILIndex + 1);
            lowLevelVirtualMachine.Run(iLGeneratedData);
            return (RET)lowLevelVirtualMachine.Pop();
        }
    }

}