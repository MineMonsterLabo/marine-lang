using System;

namespace MarineLang.CodeAnalysis
{
    public enum CompletionFilterFlags : ulong
    {
        None = UInt64.MinValue,
        Type = 0b1,
        GlobalFunction = 0b10,
        Function = 0b100,
        GlobalVariable = 0b1000,
        Variable = 0b10000,
        FunctionParameter = 0b100000,
        Keyword = 0b1000000,
        All = UInt64.MaxValue
    }
}