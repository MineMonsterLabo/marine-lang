using System.Collections.Generic;

namespace MarineLang.VirtualMachines
{
    public class NamespaceTable
    {
        private readonly Dictionary<string, NamespaceTable> childrenNamespaceTableDict;
        private readonly Dictionary<string, FuncILIndex> funcILIndexDict;

        public NamespaceTable(Dictionary<string, FuncILIndex> funcILIndexDict)
        {
            this.funcILIndexDict = funcILIndexDict;
        }

        public bool ContainFunc(string funcName)
        {
            return funcILIndexDict.ContainsKey(funcName);
        }

        public void SetFuncIlIndex(string funcName, int index)
        {
            if (funcILIndexDict.TryGetValue(funcName, out FuncILIndex funcILIndex))
            {
                funcILIndex.Index = index;
            }
            else
            {
                funcILIndexDict[funcName] = new FuncILIndex { Index = index };
            }
        }

        public FuncILIndex GetFuncIlIndex(string funcName)
        {
            return funcILIndexDict[funcName];
        }
    }
}
