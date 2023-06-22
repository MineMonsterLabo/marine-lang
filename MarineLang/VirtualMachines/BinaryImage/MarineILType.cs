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
        public static MarineILType GetMarineILType(this IMarineIL il)
        {
            switch (il)
            {
                case NoOpIL _:
                    return MarineILType.NoOpIL;
                case StaticCSharpFieldLoadIL _:
                    return MarineILType.StaticCSharpFieldLoadIL;
                case StaticCSharpFieldStoreIL _:
                    return MarineILType.StaticCSharpFieldStoreIL;
                case InstanceCSharpFieldLoadIL _:
                    return MarineILType.InstanceCSharpFieldLoadIL;
                case InstanceCSharpFieldStoreIL _:
                    return MarineILType.InstanceCSharpFieldStoreIL;
                case CSharpFuncCallIL _:
                    return MarineILType.CSharpFuncCallIL;
                case StaticCSharpFuncCallIL _:
                    return MarineILType.StaticCSharpFuncCallIL;
                case InstanceCSharpFuncCallIL _:
                    return MarineILType.InstanceCSharpFuncCallIL;
                case MarineFuncCallIL _:
                    return MarineILType.MarineFuncCallIL;
                case MoveNextIL _:
                    return MarineILType.MoveNextIL;
                case GetIterCurrentL _:
                    return MarineILType.GetIterCurrentIL;
                case InstanceCSharpIndexerLoadIL _:
                    return MarineILType.InstanceCSharpIndexerLoadIL;
                case InstanceCSharpIndexerStoreIL _:
                    return MarineILType.InstanceCSharpIndexerStoreIL;
                case BinaryOpIL _:
                    return MarineILType.BinaryOpIL;
                case UnaryOpIL _:
                    return MarineILType.UnaryOpIL;
                case RetIL _:
                    return MarineILType.RetIL;
                case JumpFalseIL _:
                    return MarineILType.JumpFalseIL;
                case JumpFalseNoPopIL _:
                    return MarineILType.JumpFalseNoPopIL;
                case JumpTrueNoPopIL _:
                    return MarineILType.JumpTrueNoPopIL;
                case JumpIL _:
                    return MarineILType.JumpIL;
                case BreakIL _:
                    return MarineILType.BreakIL;
                case StoreValueIL _:
                    return MarineILType.StoreValueIL;
                case PushValueIL _:
                    return MarineILType.PushValueIL;
                case StoreIL _:
                    return MarineILType.StoreIL;
                case LoadIL _:
                    return MarineILType.LoadIL;
                case PopIL _:
                    return MarineILType.PopIL;
                case CreateArrayIL _:
                    return MarineILType.CreateArrayIL;
                case StackAllocIL _:
                    return MarineILType.StackAllocIL;
                case YieldIL _:
                    return MarineILType.YieldIL;
                case PushYieldCurrentRegisterIL _:
                    return MarineILType.PushYieldCurrentRegisterIL;
                case PushDebugContextIL _:
                    return MarineILType.PushDebugContextIL;
                case PopDebugContextIL _:
                    return MarineILType.PopDebugContextIL;
                default:
                    return MarineILType.NoOpIL;
            }
        }
    }
}