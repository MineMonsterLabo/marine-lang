using MarineLang.Models.Asts;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.VirtualMachines
{
    public class FuncScopeVariables
    {
        private readonly int baseLocalVariableIdx;
        private readonly List<Dictionary<string, int>> variableDictList = new List<Dictionary<string, int>>();
        private readonly Dictionary<string, int> globalVariableDict;
        private readonly Dictionary<string, int> captureVariableDict;
        private int localVariableCount  = 0;

        public int MaxLocalVariableCount { get; private set; } = 0;

        public FuncScopeVariables(VariableAst[] varArgs, string[] globalVariableNames, string[] captureVariableNames = null)
        {
            variableDictList.Add(
                Enumerable.Range(0, varArgs.Length)
                .ToDictionary(idx => varArgs[idx].VarName, idx => idx + 1)
            );

            globalVariableDict = Enumerable.Range(0, globalVariableNames.Length)
                .ToDictionary(idx => globalVariableNames[idx]);

            if (captureVariableNames != null)
                captureVariableDict = Enumerable.Range(0, captureVariableNames.Length)
                    .ToDictionary(idx => captureVariableNames[idx]);
            else
                captureVariableDict = new Dictionary<string, int>();

            baseLocalVariableIdx = variableDictList.First().Count + VirtualMachineConstants.CALL_RESTORE_STACK_FRAME + 1;
        }

        public void AddLocalVariable(string name)
        {
            variableDictList[variableDictList.Count - 1].Add(name, baseLocalVariableIdx + localVariableCount);
            IncrementLocalVariableCount();
        }

        public int? GetCaptureVariableIdx(string name)
        {
            if (captureVariableDict.TryGetValue(name, out int idx))
                return idx;
            return null;
        }

        public StackIndex GetVariableIdx(string name)
        {
            if (globalVariableDict.TryGetValue(name, out int idx))
                return new StackIndex(idx, true);

            for (var i = variableDictList.Count - 1; i >= 0; i--)
            {
                if (variableDictList[i].TryGetValue(name, out int idx2))
                    return new StackIndex(idx2, false);
            }

            throw new System.Exception("変数が存在しません");
        }

        public StackIndex CreateUnnamedLocalVariableIdx()
        {
            IncrementLocalVariableCount();
            return new StackIndex(baseLocalVariableIdx + localVariableCount - 1, false);
        }

        public bool ExistVariable(string name)
        {
            for (var i = variableDictList.Count - 1; i >= 0; i--)
            {
                if (variableDictList[i].ContainsKey(name))
                    return true;
            }
            return false;
        }

        public void InScope()
        {
            variableDictList.Add(new Dictionary<string, int>());
        }

        public void OutScope()
        {
            localVariableCount -= variableDictList[variableDictList.Count - 1].Count;
            variableDictList.RemoveAt(variableDictList.Count - 1);
        }

        private void IncrementLocalVariableCount()
        {
            localVariableCount++;
            MaxLocalVariableCount = MaxLocalVariableCount < localVariableCount ? localVariableCount : MaxLocalVariableCount;
        }
    }
}
