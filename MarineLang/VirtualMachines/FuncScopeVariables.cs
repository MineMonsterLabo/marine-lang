using MarineLang.Models.Asts;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.VirtualMachines
{
    public class FuncScopeVariables
    {
        private readonly int baseLocalVariableIdx;
        private readonly Dictionary<string, int> variableDict;
        private readonly Dictionary<string, int> globalVariableDict;
        private readonly Dictionary<string, int> captureVariableDict;

        public int LocalVariableCount { get; private set; } = 0;

        public FuncScopeVariables(VariableAst[] varArgs, string[] globalVariableNames, string[] captureVariableNames = null)
        {
            variableDict = Enumerable.Range(0, varArgs.Length)
                .ToDictionary(idx => varArgs[idx].VarName, idx => idx + 1);

            globalVariableDict = Enumerable.Range(0, globalVariableNames.Length)
                .ToDictionary(idx => globalVariableNames[idx]);

            if (captureVariableNames != null)
                captureVariableDict = Enumerable.Range(0, captureVariableNames.Length)
                    .ToDictionary(idx => captureVariableNames[idx]);
            else
                captureVariableDict = new Dictionary<string, int>();

            baseLocalVariableIdx = variableDict.Count + VirtualMachineConstants.CALL_RESTORE_STACK_FRAME + 1;
        }

        public void AddLocalVariable(string name)
        {
            variableDict.Add(name, baseLocalVariableIdx + LocalVariableCount);
            LocalVariableCount++;
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
            return new StackIndex(variableDict[name], false);
        }

        public StackIndex CreateUnnamedLocalVariableIdx()
        {
            LocalVariableCount++;
            return new StackIndex(baseLocalVariableIdx + LocalVariableCount - 1, false);
        }

        public bool ExistVariable(string name)
        {
            return variableDict.ContainsKey(name);
        }
    }
}
