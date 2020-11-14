using MarineLang.BuildInObjects;
using MarineLang.Models.Asts;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines
{
    public class HighLevelVirtualMachine
    {
        readonly Dictionary<string, MethodInfo> methodInfoDict = new Dictionary<string, MethodInfo>();
        readonly SortedDictionary<string, object> globalVariableDict = new SortedDictionary<string, object>();
        ILGenerator iLGenerator;

        public ILGeneratedData ILGeneratedData { get; private set; }
        public IReadOnlyDictionary<string, MethodInfo> GlobalFuncDict => methodInfoDict;
        public IReadOnlyDictionary<string, object> GlobalVariableDict => globalVariableDict;

        public IReadOnlyList<IMarineIL> MarineILs => ILGeneratedData?.marineILs;
        public HighLevelVirtualMachine()
        {
            GlobalVariableRegister("action_object_generator", new ActionObjectGenerator(this));
        }

        public bool ContainsMarineFunc(string funcName)
        {
            return ILGeneratedData?.funcILIndexDict?.ContainsKey(funcName) ?? false;
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
            ILGeneratedData = iLGenerator.Generate(methodInfoDict, globalVariableDict.Keys.ToArray());
        }

        public RET Run<RET>(string marineFuncName, params object[] args)
        {
            return Run<RET>(marineFuncName, args.AsEnumerable());
        }

        public RET Run<RET>(string marineFuncName, IEnumerable<object> args)
        {
            var lowLevelVirtualMachine = new LowLevelVirtualMachine();
            lowLevelVirtualMachine.Init();
            lowLevelVirtualMachine.nextILIndex = ILGeneratedData.funcILIndexDict[marineFuncName];
            foreach (var val in globalVariableDict.Values)
                lowLevelVirtualMachine.Push(val);
            lowLevelVirtualMachine.stackBaseCount = lowLevelVirtualMachine.GetStackCurrent();
            foreach (var arg in args)
                lowLevelVirtualMachine.Push(arg);
            lowLevelVirtualMachine.Push(lowLevelVirtualMachine.stackBaseCount);
            lowLevelVirtualMachine.Push(lowLevelVirtualMachine.nextILIndex + 1);
            lowLevelVirtualMachine.Run(ILGeneratedData);
            if (lowLevelVirtualMachine.yieldFlag)
                return (RET)YieldRun(lowLevelVirtualMachine);
            return (RET)lowLevelVirtualMachine.Pop();
        }

        private IEnumerable<object> YieldRun(LowLevelVirtualMachine lowLevelVirtualMachine)
        {
            while (true)
            {
                lowLevelVirtualMachine.Resume();
                if (lowLevelVirtualMachine.yieldFlag == false)
                    break;
                yield return null;
            }
            yield return lowLevelVirtualMachine.Pop();
        }

    }

}