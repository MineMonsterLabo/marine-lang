using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MarineLang.BuildInObjects;
using MarineLang.Models.Asts;
using MarineLang.VirtualMachines.Dumps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            if (lowLevelVirtualMachine.yieldFlag)
                return (RET) YieldRun(lowLevelVirtualMachine);
            return (RET) lowLevelVirtualMachine.Pop();
        }

        public void CreateDump()
        {
            CreateDump($"{Environment.CurrentDirectory}/marine_dump.json");
        }

        public void CreateDump(string filePath)
        {
            DumpSerializer serializer = new DumpSerializer();
            File.WriteAllText(filePath, serializer.Serialize(globalVariableDict));
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