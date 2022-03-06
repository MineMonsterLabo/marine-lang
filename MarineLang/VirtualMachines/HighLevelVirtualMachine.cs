using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MarineLang.BuildInObjects;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines
{
    public class HighLevelVirtualMachine
    {
        uint marineProgramUnitId = uint.MinValue;

        readonly Dictionary<string, MethodInfo> methodInfoDict = new Dictionary<string, MethodInfo>();
        readonly Dictionary<string, Type> staticTypeDict = new Dictionary<string, Type>();
        readonly SortedDictionary<string, object> globalVariableDict = new SortedDictionary<string, object>();
        readonly Dictionary<uint, MarineProgramUnit> marineProgramUnitList = new Dictionary<uint, MarineProgramUnit>();

        public ILGeneratedData ILGeneratedData { get; private set; }
        public IReadOnlyDictionary<string, MethodInfo> GlobalFuncDict => methodInfoDict;

        public IReadOnlyDictionary<string, Type> StaticTypeDict => staticTypeDict;

        public IReadOnlyDictionary<string, object> GlobalVariableDict => globalVariableDict;

        public IReadOnlyList<IMarineIL> MarineILs => ILGeneratedData?.marineILs;

        public event EventHandler<VirtualMachineStepEventArgs> StepEvent;

        public HighLevelVirtualMachine()
        {
            GlobalVariableRegister("action_object_generator", new ActionObjectGenerator(this));
        }

        public bool ContainsMarineFunc(IEnumerable<string> namespaceStrings, string funcName)
        {
            return ILGeneratedData?.namespaceTable?.ContainFunc(namespaceStrings, funcName) ?? false;
        }

        public bool ContainsMarineFunc(string funcName)
        {
            return ILGeneratedData?.namespaceTable?.ContainFunc(funcName) ?? false;
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

        public uint LoadProgram(string[] namespaceStrings, ProgramAst programAst)
        {
            marineProgramUnitList.Add(marineProgramUnitId, new MarineProgramUnit(namespaceStrings, programAst));

            return marineProgramUnitId++;
        }

        public uint LoadProgram(ProgramAst programAst)
        {
            marineProgramUnitList.Add(marineProgramUnitId, new MarineProgramUnit(new string[] { }, programAst));

            return marineProgramUnitId++;
        }

        public void Compile()
        {
            ILGeneratedData
                = new ILGenerator(marineProgramUnitList.Values).Generate(methodInfoDict, staticTypeDict,
                    globalVariableDict.Keys.ToArray());
        }

        public void ClearProgram(uint programId)
        {
            ILGeneratedData = null;
            marineProgramUnitList.Remove(programId);
        }

        public void ClearAllPrograms()
        {
            ILGeneratedData = null;
            marineProgramUnitList.Clear();
        }

        public MarineProgramUnit GetProgramUnit(uint programId)
        {
            return marineProgramUnitList[programId];
        }

        public MarineValue<RET> Run<RET>(string marineFuncName, params object[] args)
        {
            return Run<RET>(marineFuncName, args.AsEnumerable());
        }

        public MarineValue<RET> Run<RET>(IEnumerable<string> namespaceStrings, string marineFuncName,
            params object[] args)
        {
            return Run<RET>(namespaceStrings, marineFuncName, args.AsEnumerable());
        }

        public MarineValue Run(string marineFuncName, params object[] args)
        {
            return Run(Enumerable.Empty<string>(), marineFuncName, args.AsEnumerable());
        }

        public MarineValue Run(IEnumerable<string> namespaceStrings, string marineFuncName, params object[] args)
        {
            return Run(namespaceStrings, marineFuncName, args.AsEnumerable());
        }

        public MarineValue<RET> Run<RET>(string marineFuncName, IEnumerable<object> args)
        {
            return new MarineValue<RET>(Run(Enumerable.Empty<string>(), marineFuncName, args));
        }

        public MarineValue<RET> Run<RET>(IEnumerable<string> namespaceStrings, string marineFuncName,
            IEnumerable<object> args)
        {
            return new MarineValue<RET>(Run(namespaceStrings, marineFuncName, args));
        }

        public MarineValue Run(string marineFuncName, IEnumerable<object> args)
        {
            return Run(Enumerable.Empty<string>(), marineFuncName, args);
        }

        public MarineValue Run(IEnumerable<string> namespaceStrings, string marineFuncName, IEnumerable<object> args)
        {
            var lowLevelVirtualMachine = new LowLevelVirtualMachine();
            lowLevelVirtualMachine.onStepILCallback = StepEvent;
            lowLevelVirtualMachine.Init();

            try
            {
                lowLevelVirtualMachine.nextILIndex
                    = ILGeneratedData.namespaceTable.GetFuncILIndex(namespaceStrings, marineFuncName).Index;
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
            catch (MarineRuntimeException)
            {
                throw;
            }
            catch (Exception e)
            {
                var currentIL = ILGeneratedData.marineILs[lowLevelVirtualMachine.nextILIndex];
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(currentIL,e.Message), e);
            }
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
            return serializer.Serialize(methodInfoDict, staticTypeDict, globalVariableDict);
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