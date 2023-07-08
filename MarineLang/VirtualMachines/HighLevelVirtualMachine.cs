using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MarineLang.BuildInObjects;
using MarineLang.Models.Errors;
using MarineLang.VirtualMachines.BinaryImage;
using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines
{
    public class HighLevelVirtualMachine
    {
        uint marineProgramUnitId = uint.MinValue;

        readonly CsharpFuncTable methodInfoDict = new CsharpFuncTable();
        readonly Dictionary<string, Type> staticTypeDict = new Dictionary<string, Type>();
        readonly SortedDictionary<string, object> globalVariableDict = new SortedDictionary<string, object>();
        readonly Dictionary<uint, MarineProgramUnit> marineProgramUnitList = new Dictionary<uint, MarineProgramUnit>();

        public ILGeneratedData ILGeneratedData { get; private set; }
        public IReadonlyCsharpFuncTable GlobalFuncDict => methodInfoDict;

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

        public void GlobalFuncRegister(MethodInfo methodInfo, string methodName = null)
        {
            methodInfoDict.AddCsharpFunc(methodName ?? methodInfo.Name, methodInfo);
        }

        public void GlobalFuncRegister(IEnumerable<string> namespaceStrings, MethodInfo methodInfo, string methodName = null)
        {
            methodInfoDict.AddCsharpFunc(namespaceStrings, methodName ?? methodInfo.Name, methodInfo);
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

        public uint LoadProgram(MarineProgramUnit marineProgramUnit)
        {
            marineProgramUnitList.Add(marineProgramUnitId, marineProgramUnit);
            return marineProgramUnitId++;
        }

        public void Compile()
        {
            ILGeneratedData
                = new ILGenerator(marineProgramUnitList.Values).Generate(methodInfoDict, staticTypeDict,
                    globalVariableDict.Keys.ToArray());
        }

        public void LoadCompiledBinaryImage(byte[] image, ImageOptimization optimization = ImageOptimization.None)
        {
            ILGeneratedData = MarineBinaryImage.ReadImage(image, optimization);
        }

        public byte[] CreateCompiledBinaryImage(ImageOptimization optimization = ImageOptimization.None)
        {
            return MarineBinaryImage.WriteImage(ILGeneratedData, optimization);
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
            catch (MarineILRuntimeException e)
            {
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(e.ILRuntimeErrorInfo, lowLevelVirtualMachine.GetDebugContexts())
                );
            }
            catch (Exception e)
            {
                var currentIL = ILGeneratedData.marineILs[lowLevelVirtualMachine.nextILIndex];
                var iLRuntimeErrorInfo = new ILRuntimeErrorInfo(currentIL, e.Message);
                throw new MarineRuntimeException(
                    new RuntimeErrorInfo(iLRuntimeErrorInfo, lowLevelVirtualMachine.GetDebugContexts()), e
                );
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
            yield return lowLevelVirtualMachine.yieldCurrentRegister;

            while (true)
            {
                lowLevelVirtualMachine.Resume();
                if (lowLevelVirtualMachine.yieldFlag == false)
                    break;
                yield return lowLevelVirtualMachine.yieldCurrentRegister;
            }

            yield return lowLevelVirtualMachine.Pop();
        }
    }
}