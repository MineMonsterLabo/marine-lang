using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MarineLang.BuildInObjects;
using MarineLang.Models.Asts;
using MarineLang.Utils;
using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines
{
    public class HighLevelVirtualMachine
    {
        readonly Dictionary<string, MethodInfo> methodInfoDict = new Dictionary<string, MethodInfo>();
        readonly Dictionary<string, Type> staticTypeDict = new Dictionary<string, Type>();
        readonly SortedDictionary<string, object> globalVariableDict = new SortedDictionary<string, object>();
        ILGenerator iLGenerator;

        public ILGeneratedData ILGeneratedData { get; private set; }
        public IReadOnlyDictionary<string, MethodInfo> GlobalFuncDict => methodInfoDict;

        public IReadOnlyDictionary<string, Type> StaticTypeDict => staticTypeDict;

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

        public void StaticTypeRegister(Type type)
        {
            staticTypeDict.Add(type.Name, type);
        }

        public void StaticTypeRegister<T>()
        {
            staticTypeDict.Add(typeof(T).Name, typeof(T));
        }

        public void StaticTypeRegister(string alias, Type type)
        {
            staticTypeDict.Add(alias, type);
        }

        public void StaticTypeRegister<T>(string alias)
        {
            staticTypeDict.Add(alias, typeof(T));
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
            ILGeneratedData = iLGenerator.Generate(methodInfoDict, staticTypeDict, globalVariableDict.Keys.ToArray());
        }

        public MarineValue<RET> Run<RET>(string marineFuncName, params object[] args)
        {
            return Run<RET>(marineFuncName, args.AsEnumerable());
        }

        public MarineValue Run(string marineFuncName, params object[] args)
        {
            return Run(marineFuncName, args.AsEnumerable());
        }

        public MarineValue<RET> Run<RET>(string marineFuncName, IEnumerable<object> args)
        {
            return new MarineValue<RET>(Run(marineFuncName, args));
        }

        public MarineValue Run(string marineFuncName, IEnumerable<object> args)
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
                return new MarineValue(YieldRun(lowLevelVirtualMachine));
            return new MarineValue(lowLevelVirtualMachine.Pop());
        }

        public void CreateDumpWithFile()
        {
            CreateDumpWithFile($"{Environment.CurrentDirectory}/marine_dump.json");
        }

        public void CreateDumpWithFile(string filePath)
        {
            File.WriteAllText(filePath, CreateDumpWithString());
        }

        public string CreateDumpWithString()
        {
            DumpSerializer serializer = new DumpSerializer();
            return serializer.Serialize(globalVariableDict);
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