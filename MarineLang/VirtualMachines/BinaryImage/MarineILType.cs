using System;
using System.IO;
using System.Reflection;
using MarineLang.Models;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public enum MarineILType
    {
        NoOpIL = 0,
        StaticCSharpFieldLoadIL = 10,
        StaticCSharpFieldStoreIL,
        InstanceCSharpFieldLoadIL,
        InstanceCSharpFieldStoreIL,
        CSharpFuncCallIL = 20,
        StaticCSharpFuncCallIL,
        InstanceCSharpFuncCallIL,
        MarineFuncCallIL,
        MoveNextIL = 30,
        GetIterCurrentIL,
        InstanceCSharpIndexerLoadIL = 40,
        InstanceCSharpIndexerStoreIL,
        BinaryOpIL = 50,
        UnaryOpIL,
        RetIL = 60,
        JumpFalseIL,
        JumpFalseNoPopIL,
        JumpTrueNoPopIL,
        JumpIL,
        BreakIL,
        StoreValueIL,
        PushValueIL,
        StoreIL,
        LoadIL,
        PopIL,
        CreateArrayIL = 80,
        StackAllocIL,
        YieldIL = 90,
        PushYieldCurrentRegisterIL,
        PushDebugContextIL = 128,
        PopDebugContextIL,
    }

    public static class MarineILTypeIOExtensions
    {
        public static void WriteMarineIL(this MarineBinaryImageWriter writer, IMarineIL il)
        {
            switch (il)
            {
                case NoOpIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.NoOpIL);
                    break;
                case StaticCSharpFieldLoadIL staticCSharpFieldLoadIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpFieldLoadIL);
                    writer.Write(staticCSharpFieldLoadIl.type);
                    writer.Write(staticCSharpFieldLoadIl.fieldName);
                    break;
                case StaticCSharpFieldStoreIL staticCSharpFieldStoreIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpFieldStoreIL);
                    writer.Write(staticCSharpFieldStoreIl.type);
                    writer.Write(staticCSharpFieldStoreIl.fieldName);
                    break;
                case InstanceCSharpFieldLoadIL instanceCSharpFieldLoadIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpFieldLoadIL);
                    writer.Write(instanceCSharpFieldLoadIl.fieldName);
                    break;
                case InstanceCSharpFieldStoreIL instanceCSharpFieldStoreIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpFieldStoreIL);
                    writer.Write(instanceCSharpFieldStoreIl.fieldName);
                    break;
                case CSharpFuncCallIL cSharpFuncCallIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.CSharpFuncCallIL);
                    writer.Write(cSharpFuncCallIl.methodInfo);
                    writer.Write7BitEncodedIntPolyfill(cSharpFuncCallIl.argCount);
                    break;
                case StaticCSharpFuncCallIL staticCSharpFuncCallIl:
                {
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.StaticCSharpFuncCallIL);
                    writer.Write(staticCSharpFuncCallIl.type);
                    writer.Write(staticCSharpFuncCallIl.funcName);

                    var methods = staticCSharpFuncCallIl.methodInfos;
                    writer.Write7BitEncodedIntPolyfill(methods.Length);
                    foreach (var method in methods)
                    {
                        writer.Write(method);
                    }

                    var genericTypes = staticCSharpFuncCallIl.genericTypes;
                    writer.Write7BitEncodedIntPolyfill(genericTypes.Length);
                    foreach (var genericType in genericTypes)
                    {
                        writer.Write(genericType);
                    }

                    writer.Write7BitEncodedIntPolyfill(staticCSharpFuncCallIl.argCount);

                    break;
                }
                case InstanceCSharpFuncCallIL instanceCSharpFuncCallIl:
                {
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpFuncCallIL);
                    writer.Write(instanceCSharpFuncCallIl.funcName);

                    var genericTypes = instanceCSharpFuncCallIl.genericTypes;
                    writer.Write7BitEncodedIntPolyfill(genericTypes.Length);
                    foreach (var genericType in genericTypes)
                    {
                        writer.Write(genericType);
                    }

                    writer.Write7BitEncodedIntPolyfill(instanceCSharpFuncCallIl.argCount);

                    break;
                }
                case MarineFuncCallIL marineFuncCallIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.MarineFuncCallIL);
                    writer.Write(marineFuncCallIl.funcName);
                    writer.Write7BitEncodedIntPolyfill(marineFuncCallIl.funcILIndex.Index);
                    writer.Write7BitEncodedIntPolyfill(marineFuncCallIl.argCount);
                    break;
                case MoveNextIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.MoveNextIL);
                    break;
                case GetIterCurrentL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.GetIterCurrentIL);
                    break;
                case InstanceCSharpIndexerLoadIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpIndexerLoadIL);
                    break;
                case InstanceCSharpIndexerStoreIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.InstanceCSharpIndexerStoreIL);
                    break;
                case BinaryOpIL binaryOpIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.BinaryOpIL);
                    writer.Write7BitEncodedIntPolyfill((int)binaryOpIl.opKind);
                    break;
                case UnaryOpIL unaryOpIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.UnaryOpIL);
                    writer.Write7BitEncodedIntPolyfill((int)unaryOpIl.opKind);
                    break;
                case RetIL retIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.RetIL);
                    writer.Write7BitEncodedIntPolyfill(retIl.argCount);
                    break;
                case JumpFalseIL jumpFalseIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.JumpFalseIL);
                    writer.Write7BitEncodedIntPolyfill(jumpFalseIl.nextILIndex);
                    break;
                case JumpFalseNoPopIL jumpFalseNoPopIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.JumpFalseNoPopIL);
                    writer.Write7BitEncodedIntPolyfill(jumpFalseNoPopIl.nextILIndex);
                    break;
                case JumpTrueNoPopIL jumpTrueNoPopIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.JumpTrueNoPopIL);
                    writer.Write7BitEncodedIntPolyfill(jumpTrueNoPopIl.nextILIndex);
                    break;
                case JumpIL jumpIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.JumpIL);
                    writer.Write7BitEncodedIntPolyfill(jumpIl.nextILIndex);
                    break;
                case BreakIL breakIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.BreakIL);
                    writer.Write7BitEncodedIntPolyfill(breakIl.breakIndex.Index);
                    break;
                // case StoreValueIL storeValueIl:
                //     writer.Write7BitEncodedIntPolyfill((int)MarineILType.StoreValueIL);
                //     writer.WriteConstValue(storeValueIl.value);
                //     writer.Write(storeValueIl.stackIndex);
                //     break;
                case PushValueIL pushValueIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.PushValueIL);
                    writer.WriteConstValue(pushValueIl.value);
                    break;
                case StoreIL storeIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.StoreIL);
                    writer.Write(storeIl.stackIndex);
                    break;
                case LoadIL loadIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.LoadIL);
                    writer.Write(loadIl.stackIndex);
                    break;
                case PopIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.PopIL);
                    break;
                case CreateArrayIL createArray:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.CreateArrayIL);
                    writer.Write7BitEncodedIntPolyfill(createArray.initSize);
                    writer.Write7BitEncodedIntPolyfill(createArray.size);
                    break;
                case StackAllocIL stackAllocIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.StackAllocIL);
                    writer.Write7BitEncodedIntPolyfill(stackAllocIl.size);
                    break;
                case YieldIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.YieldIL);
                    break;
                case PushYieldCurrentRegisterIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.PushYieldCurrentRegisterIL);
                    break;
                case PushDebugContextIL pushDebugContext:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.PushDebugContextIL);
                    writer.Write(pushDebugContext.debugContext);
                    break;
                case PopDebugContextIL _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.PopDebugContextIL);
                    break;

                default:
                    throw new InvalidDataException();
            }
        }

        public static IMarineIL ReadMarineIL(this MarineBinaryImageReader reader)
        {
            var ilType = (MarineILType)reader.Read7BitEncodedIntPolyfill();
            switch (ilType)
            {
                case MarineILType.NoOpIL:
                    return new NoOpIL();
                case MarineILType.StaticCSharpFieldLoadIL:
                {
                    var type = reader.ReadType();
                    var fieldName = reader.ReadString();
                    return new StaticCSharpFieldLoadIL(type, fieldName);
                }
                case MarineILType.StaticCSharpFieldStoreIL:
                {
                    var type = reader.ReadType();
                    var fieldName = reader.ReadString();
                    return new StaticCSharpFieldStoreIL(type, fieldName);
                }
                case MarineILType.InstanceCSharpFieldLoadIL:
                {
                    var fieldName = reader.ReadString();
                    return new InstanceCSharpFieldLoadIL(fieldName);
                }
                case MarineILType.InstanceCSharpFieldStoreIL:
                {
                    var fieldName = reader.ReadString();
                    return new InstanceCSharpFieldStoreIL(fieldName);
                }
                case MarineILType.CSharpFuncCallIL:
                {
                    var methodInfo = reader.ReadMethodInfo();
                    var argCount = reader.Read7BitEncodedIntPolyfill();
                    return new CSharpFuncCallIL(methodInfo, argCount);
                }
                case MarineILType.StaticCSharpFuncCallIL:
                {
                    var type = reader.ReadType();
                    var funcName = reader.ReadString();
                    var methodCount = reader.Read7BitEncodedIntPolyfill();
                    var methods = new MethodInfo[methodCount];
                    for (int i = 0; i < methodCount; i++)
                    {
                        methods[i] = reader.ReadMethodInfo();
                    }

                    var typeCount = reader.Read7BitEncodedIntPolyfill();
                    var types = new Type[typeCount];
                    for (int i = 0; i < typeCount; i++)
                    {
                        types[i] = reader.ReadType();
                    }

                    var argCount = reader.Read7BitEncodedIntPolyfill();

                    return new StaticCSharpFuncCallIL(type, methods, funcName, argCount, types);
                }
                case MarineILType.InstanceCSharpFuncCallIL:
                {
                    var funcName = reader.ReadString();
                    var typeCount = reader.Read7BitEncodedIntPolyfill();
                    var types = new Type[typeCount];
                    for (int i = 0; i < typeCount; i++)
                    {
                        types[i] = reader.ReadType();
                    }

                    var argCount = reader.Read7BitEncodedIntPolyfill();

                    return new InstanceCSharpFuncCallIL(funcName, argCount, types);
                }
                case MarineILType.MarineFuncCallIL:
                {
                    var funcName = reader.ReadString();
                    var ilIndex = new FuncILIndex
                    {
                        Index = reader.Read7BitEncodedIntPolyfill()
                    };
                    var argCount = reader.Read7BitEncodedIntPolyfill();
                    return new MarineFuncCallIL(funcName, ilIndex, argCount);
                }
                case MarineILType.MoveNextIL:
                    return new MoveNextIL();
                case MarineILType.GetIterCurrentIL:
                    return new GetIterCurrentL();
                case MarineILType.InstanceCSharpIndexerLoadIL:
                    return new InstanceCSharpIndexerLoadIL();
                case MarineILType.InstanceCSharpIndexerStoreIL:
                    return new InstanceCSharpIndexerStoreIL();
                case MarineILType.BinaryOpIL:
                {
                    var opKind = (TokenType)reader.Read7BitEncodedIntPolyfill();
                    return new BinaryOpIL(opKind);
                }
                case MarineILType.UnaryOpIL:
                {
                    var opKind = (TokenType)reader.Read7BitEncodedIntPolyfill();
                    return new UnaryOpIL(opKind);
                }
                case MarineILType.RetIL:
                {
                    var argCount = reader.Read7BitEncodedIntPolyfill();
                    return new RetIL(argCount);
                }
                case MarineILType.JumpFalseIL:
                {
                    var ilIndex = reader.Read7BitEncodedIntPolyfill();
                    return new JumpFalseIL(ilIndex);
                }
                case MarineILType.JumpFalseNoPopIL:
                {
                    var ilIndex = reader.Read7BitEncodedIntPolyfill();
                    return new JumpFalseNoPopIL(ilIndex);
                }
                case MarineILType.JumpTrueNoPopIL:
                {
                    var ilIndex = reader.Read7BitEncodedIntPolyfill();
                    return new JumpTrueNoPopIL(ilIndex);
                }
                case MarineILType.JumpIL:
                {
                    var ilIndex = reader.Read7BitEncodedIntPolyfill();
                    return new JumpIL(ilIndex);
                }
                case MarineILType.BreakIL:
                {
                    var breakIndex = new BreakIndex
                    {
                        Index = reader.Read7BitEncodedIntPolyfill()
                    };
                    return new BreakIL(breakIndex);
                }
                // case MarineILType.StoreValueIL:
                //     return new StoreValueIL();
                case MarineILType.PushValueIL:
                {
                    var value = reader.ReadConstValue();
                    return new PushValueIL(value);
                }
                case MarineILType.StoreIL:
                {
                    var stackIndex = reader.ReadStackIndex();
                    return new StoreIL(stackIndex);
                }
                case MarineILType.LoadIL:
                {
                    var stackIndex = reader.ReadStackIndex();
                    return new LoadIL(stackIndex);
                }
                case MarineILType.PopIL:
                    return new PopIL();
                case MarineILType.CreateArrayIL:
                {
                    var initSize = reader.Read7BitEncodedIntPolyfill();
                    var size = reader.Read7BitEncodedIntPolyfill();
                    return new CreateArrayIL(initSize, size);
                }
                case MarineILType.StackAllocIL:
                {
                    var size = reader.Read7BitEncodedIntPolyfill();
                    return new StackAllocIL(size);
                }
                case MarineILType.YieldIL:
                    return new YieldIL();
                case MarineILType.PushYieldCurrentRegisterIL:
                    return new PushYieldCurrentRegisterIL();
                case MarineILType.PushDebugContextIL:
                {
                    var debugContext = reader.ReadDebugContext();
                    return new PushDebugContextIL(debugContext);
                }
                case MarineILType.PopDebugContextIL:
                    return new PopDebugContextIL();
                default:
                    throw new InvalidDataException();
            }
        }
    }
}