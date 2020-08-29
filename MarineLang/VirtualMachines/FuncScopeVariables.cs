using MarineLang.Models;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.VirtualMachines
{
    public class FuncScopeVariables
    {
        private readonly int baseLocalVariableIdx;
        private readonly Dictionary<string, int> variableDict;
        private readonly Dictionary<string, int> globalVariableDict;

        public int LocalVariableCount { get; private set; } = 0;

        public FuncScopeVariables(VariableAst[] varArgs, string[] globalVariableNames)
        {
            variableDict = Enumerable.Range(0, varArgs.Length)
                .ToDictionary(idx => varArgs[idx].varName, idx => idx + 1);

            globalVariableDict = Enumerable.Range(0, globalVariableNames.Length)
                .ToDictionary(idx => globalVariableNames[idx]);

            baseLocalVariableIdx = variableDict.Count + VirtualMachineConstants.CALL_RESTORE_STACK_FRAME + 1;
        }

        public void AddLocalVariable(string name)
        {
            variableDict.Add(name, baseLocalVariableIdx + LocalVariableCount);
            LocalVariableCount++;
        }

        public int GeVariableIdx(string name)
        {
            if (globalVariableDict.TryGetValue(name, out int idx))
                return idx;
            return variableDict[name];
        }

        public int CreateUnnamedLocalVariableIdx()
        {
            LocalVariableCount++;
            return baseLocalVariableIdx + LocalVariableCount - 1;
        }
    }
}
