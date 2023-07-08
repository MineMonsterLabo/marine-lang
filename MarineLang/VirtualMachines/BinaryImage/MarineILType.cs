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
        StaticCSharpConstructorCallIL,
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
}