using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MarineLang.Models;
using MarineLang.VirtualMachines.MarineILs;
using MineUtil;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImageWriter : BinaryWriter
    {
        public ImageOptimization Optimization { get; set; }

        public MarineBinaryImageWriter(Stream input) : base(input)
        {
        }

        public MarineBinaryImageWriter(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public MarineBinaryImageWriter(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding,
            leaveOpen)
        {
        }

        public void WriteImage(ILGeneratedData data)
        {
            if (!Optimization.HasFlag(ImageOptimization.NoHeaderAndMeta))
            {
                WriteHeader();
                WriteVersion();

                WriteMetadata();
            }

            WriteNamespaceTable(data.namespaceTable);
            WriteMarineILs(data.marineILs);
        }

        private void WriteHeader()
        {
            Write(MarineBinaryImage.ImageHeader);
        }

        private void WriteVersion()
        {
            Write(MarineBinaryImage.IMAGE_VERSION);
        }

        private void WriteMetadata()
        {
            var metadata = new Dictionary<string, string>();

            var version =
                $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}";
            metadata[MarineBinaryImage.BUILD_MARINE_VERSION_KEY] = version;
            metadata[MarineBinaryImage.BUILD_TIME_STAMP_KEY] = DateTime.Now.Ticks.ToString();

            OverrideMetadata(metadata);

            this.Write7BitEncodedIntPolyfill(metadata.Count);
            foreach (var pair in metadata)
            {
                Write(pair.Key);
                Write(pair.Value);
            }
        }

        protected virtual void OverrideMetadata(Dictionary<string, string> metadata)
        {
            // Override metadata code here.
        }

        private void WriteNamespaceTable(NamespaceTable namespaceTable)
        {
            namespaceTable.WriteBinaryFormat(this);
        }

        protected virtual void WriteMarineILs(IReadOnlyList<IMarineIL> ilData)
        {
            if (Optimization.HasFlag(ImageOptimization.NoDebug))
            {
                this.Write7BitEncodedIntPolyfill(ilData.Count(il => il.IsDebugIL()));
            }
            else
            {
                this.Write7BitEncodedIntPolyfill(ilData.Count);
            }
            
            foreach (var il in ilData)
            {
                if (Optimization.HasFlag(ImageOptimization.NoDebug) && il.IsDebugIL())
                    continue;

                this.WriteMarineIL(il);
            }
        }
        
        public void WriteMarineIL(IMarineIL il)
        {
            switch (il)
            {
                case NoOpIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.NoOpIL);
                    break;
                case StaticCSharpFieldLoadIL staticCSharpFieldLoadIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpFieldLoadIL);
                    Write(staticCSharpFieldLoadIl.type);
                    Write(staticCSharpFieldLoadIl.fieldName);
                    break;
                case StaticCSharpFieldStoreIL staticCSharpFieldStoreIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpFieldStoreIL);
                    Write(staticCSharpFieldStoreIl.type);
                    Write(staticCSharpFieldStoreIl.fieldName);
                    break;
                case InstanceCSharpFieldLoadIL instanceCSharpFieldLoadIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpFieldLoadIL);
                    Write(instanceCSharpFieldLoadIl.fieldName);
                    break;
                case InstanceCSharpFieldStoreIL instanceCSharpFieldStoreIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpFieldStoreIL);
                    Write(instanceCSharpFieldStoreIl.fieldName);
                    break;
                case CSharpFuncCallIL cSharpFuncCallIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.CSharpFuncCallIL);
                    Write(cSharpFuncCallIl.methodInfo);
                    this.Write7BitEncodedIntPolyfill(cSharpFuncCallIl.argCount);
                    break;
                case StaticCSharpFuncCallIL staticCSharpFuncCallIl:
                {
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpFuncCallIL);
                    Write(staticCSharpFuncCallIl.type);
                    Write(staticCSharpFuncCallIl.funcName);

                    var methods = staticCSharpFuncCallIl.methodInfos;
                    this.Write7BitEncodedIntPolyfill(methods.Length);
                    foreach (var method in methods)
                    {
                        Write(method);
                    }

                    var genericTypes = staticCSharpFuncCallIl.genericTypes;
                    this.Write7BitEncodedIntPolyfill(genericTypes.Length);
                    foreach (var genericType in genericTypes)
                    {
                        Write(genericType);
                    }

                    this.Write7BitEncodedIntPolyfill(staticCSharpFuncCallIl.argCount);

                    break;
                }
                case InstanceCSharpFuncCallIL instanceCSharpFuncCallIl:
                {
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpFuncCallIL);
                    Write(instanceCSharpFuncCallIl.funcName);

                    var genericTypes = instanceCSharpFuncCallIl.genericTypes;
                    Write(genericTypes != null);
                    if (genericTypes != null)
                    {
                        this.Write7BitEncodedIntPolyfill(genericTypes.Length);
                        foreach (var genericType in genericTypes)
                        {
                            Write(genericType);
                        }
                    }

                    this.Write7BitEncodedIntPolyfill(instanceCSharpFuncCallIl.argCount);

                    break;
                }
                case MarineFuncCallIL marineFuncCallIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.MarineFuncCallIL);
                    Write(marineFuncCallIl.funcName);
                    this.Write7BitEncodedIntPolyfill(marineFuncCallIl.funcILIndex.Index);
                    this.Write7BitEncodedIntPolyfill(marineFuncCallIl.argCount);
                    break;
                case StaticCSharpConstructorCallIL constructorCallIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpConstructorCallIL);
                    Write(constructorCallIl.type);
                    Write(constructorCallIl.funcName);

                    var constructors = constructorCallIl.constructorInfos;
                    this.Write7BitEncodedIntPolyfill(constructors.Length);
                    foreach (var constructor in constructors)
                    {
                        Write(constructor);
                    }

                    this.Write7BitEncodedIntPolyfill(constructorCallIl.argCount);
                    break;
                case MoveNextIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.MoveNextIL);
                    break;
                case GetIterCurrentIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.GetIterCurrentIL);
                    break;
                case InstanceCSharpIndexerLoadIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpIndexerLoadIL);
                    break;
                case InstanceCSharpIndexerStoreIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpIndexerStoreIL);
                    break;
                case BinaryOpIL binaryOpIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.BinaryOpIL);
                    this.Write7BitEncodedIntPolyfill((int)binaryOpIl.opKind);
                    break;
                case UnaryOpIL unaryOpIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.UnaryOpIL);
                    this.Write7BitEncodedIntPolyfill((int)unaryOpIl.opKind);
                    break;
                case RetIL retIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.RetIL);
                    this.Write7BitEncodedIntPolyfill(retIl.argCount);
                    break;
                case JumpFalseIL jumpFalseIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.JumpFalseIL);
                    this.Write7BitEncodedIntPolyfill(jumpFalseIl.nextILIndex);
                    break;
                case JumpFalseNoPopIL jumpFalseNoPopIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.JumpFalseNoPopIL);
                    this.Write7BitEncodedIntPolyfill(jumpFalseNoPopIl.nextILIndex);
                    break;
                case JumpTrueNoPopIL jumpTrueNoPopIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.JumpTrueNoPopIL);
                    this.Write7BitEncodedIntPolyfill(jumpTrueNoPopIl.nextILIndex);
                    break;
                case JumpIL jumpIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.JumpIL);
                    this.Write7BitEncodedIntPolyfill(jumpIl.nextILIndex);
                    break;
                case BreakIL breakIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.BreakIL);
                    this.Write7BitEncodedIntPolyfill(breakIl.breakIndex.Index);
                    break;
                // case StoreValueIL storeValueIl:
                //     Write7BitEncodedIntPolyfill((int)MarineILType.StoreValueIL);
                //     WriteConstValue(storeValueIl.value);
                //     Write(storeValueIl.stackIndex);
                //     break;
                case PushValueIL pushValueIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.PushValueIL);
                    WriteConstValue(pushValueIl.value);
                    break;
                case StoreIL storeIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.StoreIL);
                    Write(storeIl.stackIndex);
                    break;
                case LoadIL loadIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.LoadIL);
                    Write(loadIl.stackIndex);
                    break;
                case PopIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.PopIL);
                    break;
                case CreateArrayIL createArray:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.CreateArrayIL);
                    this.Write7BitEncodedIntPolyfill(createArray.initSize);
                    this.Write7BitEncodedIntPolyfill(createArray.size);
                    break;
                case StackAllocIL stackAllocIl:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.StackAllocIL);
                    this.Write7BitEncodedIntPolyfill(stackAllocIl.size);
                    break;
                case YieldIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.YieldIL);
                    break;
                case PushYieldCurrentRegisterIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.PushYieldCurrentRegisterIL);
                    break;
                case PushDebugContextIL pushDebugContext:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.PushDebugContextIL);
                    Write(pushDebugContext.debugContext);
                    break;
                case PopDebugContextIL _:
                    this.Write7BitEncodedIntPolyfill((int)MarineILType.PopDebugContextIL);
                    break;

                default:
                    throw new InvalidDataException();
            }
        }

        public virtual void Write(Type type)
        {
            Write(type.AssemblyQualifiedName);
        }

        public virtual void Write(MethodInfo methodInfo)
        {
            Write(methodInfo.DeclaringType);
            Write(methodInfo.Name);

            var parameters = methodInfo.GetParameters();
            this.Write7BitEncodedIntPolyfill(parameters.Length);
            foreach (var parameter in parameters)
            {
                Write(parameter.ParameterType.IsGenericParameter);
                if (!parameter.ParameterType.IsGenericParameter)
                    Write(parameter.ParameterType);
                else
                    throw new NotSupportedException();
            }
        }

        public virtual void Write(ConstructorInfo constructorInfo)
        {
            Write(constructorInfo.DeclaringType);

            var parameters = constructorInfo.GetParameters();
            this.Write7BitEncodedIntPolyfill(parameters.Length);
            foreach (var parameter in parameters)
            {
                Write(parameter.ParameterType.IsGenericParameter);
                if (!parameter.ParameterType.IsGenericParameter)
                    Write(parameter.ParameterType);
                else
                    throw new NotSupportedException();
            }
        }

        public void Write(StackIndex stackIndex)
        {
            Write(stackIndex.isAbsolute);
            this.Write7BitEncodedIntPolyfill(stackIndex.index);
        }

        public void Write(DebugContext debugContext)
        {
            Write(debugContext.ProgramUnitName);
            Write(debugContext.FuncName);
            Write(debugContext.RangePosition.Start);
            Write(debugContext.RangePosition.End);
        }

        public void Write(Position position)
        {
            this.Write7BitEncodedIntPolyfill(position.column);
            this.Write7BitEncodedIntPolyfill(position.line);
            this.Write7BitEncodedIntPolyfill(position.index);
        }
        
        public void WriteConstValue(object value)
        {
            if (value == null)
            {
                this.Write7BitEncodedIntPolyfill((int)MarineConstType.Null);
                return;
            }

            switch (value)
            {
                case bool b:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.Bool);
                    this.Write(b);
                    break;
                case int i:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.Int);
                    this.Write7BitEncodedIntPolyfill(i);
                    break;
                case float f:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.Float);
                    Write(f);
                    break;
                case char c:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.Char);
                    Write(c);
                    break;
                case string s:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.String);
                    Write(s);
                    break;
                case Enum e:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.Enum);
                    Write(e.GetType());
                    this.Write7BitEncodedInt64Polyfill(Convert.ToInt64(e));
                    break;
                case Unit _:
                    this.Write7BitEncodedIntPolyfill((int)MarineConstType.Unit);
                    break;

                default:
                    throw new InvalidDataException();
            }
        }
    }
}