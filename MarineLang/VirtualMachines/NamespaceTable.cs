using System.Collections.Generic;

namespace MarineLang.VirtualMachines
{
    public class NamespaceTable
    {
        private readonly Dictionary<string, NamespaceTable> childrenNamespaceTableDict = new Dictionary<string, NamespaceTable>();
        private readonly Dictionary<string, FuncILIndex> funcILIndexDict = new Dictionary<string, FuncILIndex>();

        public bool ContainFunc(string funcName)
        {
            return funcILIndexDict.ContainsKey(funcName);
        }

        public IEnumerable<NamespaceTable> GetChildrenNamespaces()
        {
            return childrenNamespaceTableDict.Values;
        }

        public IEnumerable<string> GetFuncNames()
        {
            return funcILIndexDict.Keys;
        }

        public NamespaceTable GetChildNamespace(IEnumerable<string> namespaceStrings)
        {
            return GetChildNamespace(namespaceStrings.GetEnumerator());
        }

        public NamespaceTable GetChildNamespace(IEnumerator<string> namespaceStrings)
        {
            if (namespaceStrings.MoveNext())
            {
                return childrenNamespaceTableDict[namespaceStrings.Current].GetChildNamespace(namespaceStrings);
            }

            return this;
        }

        public void AddFuncIlIndex(IEnumerable<string> namespaceStrings, IEnumerable<string> funcNames)
        {
            AddFuncIlIndex(namespaceStrings.GetEnumerator(), funcNames);
        }

        public void AddFuncIlIndex(IEnumerator<string> namespaceStrings, IEnumerable<string> funcNames)
        {
            if (namespaceStrings.MoveNext())
            {
                if (childrenNamespaceTableDict.ContainsKey(namespaceStrings.Current) == false)
                {
                    childrenNamespaceTableDict[namespaceStrings.Current] = new NamespaceTable();
                }

                childrenNamespaceTableDict[namespaceStrings.Current].AddFuncIlIndex(namespaceStrings, funcNames);
            }
            else
            {
                foreach (var funcName in funcNames)
                {
                    SetFuncIlIndex(funcName, -1);
                }
            }
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

        public FuncILIndex GetFuncIlIndex(IEnumerable<string> namespaceStrings, string funcName)
        {
            return GetFuncIlIndex(namespaceStrings.GetEnumerator(), funcName);
        }

        public FuncILIndex GetFuncIlIndex(IEnumerator<string> namespaceStrings, string funcName)
        {
            if (namespaceStrings.MoveNext())
            {
                return childrenNamespaceTableDict[namespaceStrings.Current].GetFuncIlIndex(namespaceStrings, funcName);
            }

            return funcILIndexDict[funcName];
        }
    }
}
