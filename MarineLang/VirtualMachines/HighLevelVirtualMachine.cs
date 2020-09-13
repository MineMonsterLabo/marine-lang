using MarineLang.BuildInObjects;
using MarineLang.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines
{
    public class HighLevelVirtualMachine
    {
        Dictionary<string, MethodInfo> methodInfoDict = new Dictionary<string, MethodInfo>();
        SortedDictionary<string, object> globalVariableDict = new SortedDictionary<string, object>();
        ILGeneratedData iLGeneratedData;
        ILGenerator iLGenerator;

        public IReadOnlyList<IMarineIL> MarineILs => iLGeneratedData?.marineILs;

        public HighLevelVirtualMachine()
        {
            GlobalVariableRegister("action_object_generator", new ActionObjectGenerator(this));
        }

        public void GlobalFuncRegister(MethodInfo methodInfo)
        {
            methodInfoDict.Add(methodInfo.Name, methodInfo);
        }

        public void GlobalVariableRegister(string name, object val)
        {
            globalVariableDict.Add(name, val);
        }

        public void SetProgram(ProgramAst programAst)
        {
            iLGenerator = new ILGenerator(programAst);
        }

        public void Compile()
        {
            iLGeneratedData = iLGenerator.Generate(methodInfoDict, globalVariableDict.Keys.ToArray());
        }

        public RET Run<RET>(string marineFuncName, params object[] args)
        {
            return Run<RET>(marineFuncName, args.AsEnumerable());
        }

        public RET Run<RET>(string marineFuncName, IEnumerable<object> args)
        {
            var lowLevelVirtualMachine = new LowLevelVirtualMachine();
            lowLevelVirtualMachine.Init();
            lowLevelVirtualMachine.nextILIndex = iLGeneratedData.funcILIndexDict[marineFuncName];
            foreach (var val in globalVariableDict.Values)
                lowLevelVirtualMachine.Push(val);
            lowLevelVirtualMachine.stackBaseCount = lowLevelVirtualMachine.GetStackCurrent();
            foreach (var arg in args)
                lowLevelVirtualMachine.Push(arg);
            lowLevelVirtualMachine.Push(lowLevelVirtualMachine.stackBaseCount);
            lowLevelVirtualMachine.Push(lowLevelVirtualMachine.nextILIndex + 1);
            lowLevelVirtualMachine.Run(iLGeneratedData);
            return (RET)lowLevelVirtualMachine.Pop();
        }

    }

}