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
        CreateArrayIL = 70,
        StackAllocIL,
        YieldIL = 80,
        PushYieldCurrentRegisterIL,
        PushDebugContextIL = 128,
        PopDebugContextIL,
    }

    public static class MarineILTypeExtension
    {
        public static void WriteMarineIL(this IMarineIL il, MarineBinaryImageWriter writer)
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
                case StoreValueIL storeValueIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.StoreValueIL);
                    storeValueIl.value.WriteConstValue(writer);
                    writer.Write(storeValueIl.stackIndex);
                    break;
                case PushValueIL pushValueIl:
                    writer.Write7BitEncodedIntPolyfill((int)MarineILType.PushValueIL);
                    pushValueIl.value.WriteConstValue(writer);
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
            }
        }
    }
}