using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MarineLang.Models;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImageWriter : BinaryWriter
    {
        public bool IsDebugMode { get; set; } = true;

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
            WriteHeader();
            WriteVersion();

            WriteMetadata();

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
            this.Write7BitEncodedIntPolyfill(ilData.Count);
            foreach (var il in ilData)
            {
                WriteMarineIL(il);
            }
        }

        protected virtual void WriteMarineIL(IMarineIL il)
        {
            if (!IsDebugMode)
                if (il is PushDebugContextIL || il is PopDebugContextIL)
                    return;

            Write7BitEncodedInt((int)il.GetMarineILType());
            switch (il)
            {
                case NoOpIL _:
                    // empty
                    break;
                case StaticCSharpFieldLoadIL staticCSharpFieldLoadIl:
                    Write(staticCSharpFieldLoadIl.type);
                    Write(staticCSharpFieldLoadIl.fieldName);
                    break;
                case StaticCSharpFieldStoreIL staticCSharpFieldStoreIl:
                    Write(staticCSharpFieldStoreIl.type);
                    Write(staticCSharpFieldStoreIl.fieldName);
                    break;
                case InstanceCSharpFieldLoadIL instanceCSharpFieldLoadIl:
                    Write(instanceCSharpFieldLoadIl.fieldName);
                    break;
                case InstanceCSharpFieldStoreIL instanceCSharpFieldStoreIl:
                    Write(instanceCSharpFieldStoreIl.fieldName);
                    break;
                case CSharpFuncCallIL cSharpFuncCallIl:
                    Write(cSharpFuncCallIl.methodInfo);
                    Write7BitEncodedInt(cSharpFuncCallIl.argCount);
                    break;
                case StaticCSharpFuncCallIL staticCSharpFuncCallIl:
                {
                    Write(staticCSharpFuncCallIl.type);
                    Write(staticCSharpFuncCallIl.funcName);

                    var methods = staticCSharpFuncCallIl.methodInfos;
                    Write7BitEncodedInt(methods.Length);
                    foreach (var method in methods)
                    {
                        Write(method);
                    }

                    var genericTypes = staticCSharpFuncCallIl.genericTypes;
                    Write7BitEncodedInt(genericTypes.Length);
                    foreach (var genericType in genericTypes)
                    {
                        Write(genericType);
                    }

                    Write7BitEncodedInt(staticCSharpFuncCallIl.argCount);

                    break;
                }
                case InstanceCSharpFuncCallIL instanceCSharpFuncCallIl:
                {
                    Write(instanceCSharpFuncCallIl.funcName);

                    var genericTypes = instanceCSharpFuncCallIl.genericTypes;
                    Write7BitEncodedInt(genericTypes.Length);
                    foreach (var genericType in genericTypes)
                    {
                        Write(genericType);
                    }

                    Write7BitEncodedInt(instanceCSharpFuncCallIl.argCount);

                    break;
                }
                case MarineFuncCallIL marineFuncCallIl:
                    Write(marineFuncCallIl.funcName);
                    Write7BitEncodedInt(marineFuncCallIl.funcILIndex.Index);
                    Write7BitEncodedInt(marineFuncCallIl.argCount);
                    break;
                case MoveNextIL _:
                    // empty
                    break;
                case GetIterCurrentL _:
                    // empty
                    break;
                case InstanceCSharpIndexerLoadIL _:
                    // empty
                    break;
                case InstanceCSharpIndexerStoreIL _:
                    // empty
                    break;
                case BinaryOpIL binaryOpIl:
                    Write7BitEncodedInt((int)binaryOpIl.opKind);
                    break;
                case UnaryOpIL unaryOpIl:
                    Write7BitEncodedInt((int)unaryOpIl.opKind);
                    break;
                case RetIL retIl:
                    Write7BitEncodedInt(retIl.argCount);
                    break;
                case JumpFalseIL jumpFalseIl:
                    Write7BitEncodedInt(jumpFalseIl.nextILIndex);
                    break;
                case JumpFalseNoPopIL jumpFalseNoPopIl:
                    Write7BitEncodedInt(jumpFalseNoPopIl.nextILIndex);
                    break;
                case JumpTrueNoPopIL jumpTrueNoPopIl:
                    Write7BitEncodedInt(jumpTrueNoPopIl.nextILIndex);
                    break;
                case JumpIL jumpIl:
                    Write7BitEncodedInt(jumpIl.nextILIndex);
                    break;
                case BreakIL breakIl:
                    Write7BitEncodedInt(breakIl.breakIndex.Index);
                    break;
                case StoreValueIL storeValueIl:
                    Write(storeValueIl.value.GetType());
                    Write(storeValueIl.value.ToString());
                    Write(storeValueIl.stackIndex);
                    break;
                case PushValueIL pushValueIl:
                    Write(pushValueIl.value.GetType());
                    Write(pushValueIl.value.ToString());
                    break;
                case StoreIL storeIl:
                    Write(storeIl.stackIndex);
                    break;
                case LoadIL loadIl:
                    Write(loadIl.stackIndex);
                    break;
                case PopIL _:
                    // empty
                    break;
                case CreateArrayIL createArray:
                    Write7BitEncodedInt(createArray.initSize);
                    Write7BitEncodedInt(createArray.size);
                    break;
                case StackAllocIL stackAllocIl:
                    Write7BitEncodedInt(stackAllocIl.size);
                    break;
                case YieldIL _:
                    // empty
                    break;
                case PushYieldCurrentRegisterIL _:
                    // empty
                    break;
                case PushDebugContextIL pushDebugContext:
                    Write(pushDebugContext.debugContext);
                    break;
                case PopDebugContextIL popDebugContext:
                    // empty
                    break;
            }
        }

        public virtual void Write(Type type)
        {
            Write(type.AssemblyQualifiedName);
        }

        public virtual void Write(MethodInfo methodInfo)
        {
            Write(methodInfo.DeclaringType);

            var parameters = methodInfo.GetParameters();
            Write7BitEncodedInt(parameters.Length);
            foreach (var parameter in parameters)
            {
                Write(parameter.ParameterType);
            }
        }

        public virtual void Write(StackIndex stackIndex)
        {
            Write(stackIndex.isAbsolute);
            Write7BitEncodedInt(stackIndex.index);
        }

        public virtual void Write(DebugContext debugContext)
        {
            Write(debugContext.ProgramUnitName);
            Write(debugContext.FuncName);
            Write(debugContext.RangePosition.Start);
            Write(debugContext.RangePosition.End);
        }

        public virtual void Write(Position position)
        {
            Write7BitEncodedInt(position.column);
            Write7BitEncodedInt(position.line);
            Write7BitEncodedInt(position.index);
        }
    }
}