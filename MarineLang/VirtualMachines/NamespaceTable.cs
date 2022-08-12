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

        public bool ContainFunc(IEnumerable<string> namespaceStrings, string funcName)
        {
            return ContainFunc(namespaceStrings.GetEnumerator(), funcName);
        }

        public bool ContainFunc(IEnumerator<string> namespaceStrings, string funcName)
        {
            if (namespaceStrings.MoveNext())
            {
                return
                    childrenNamespaceTableDict.ContainsKey(namespaceStrings.Current) &&
                    childrenNamespaceTableDict[namespaceStrings.Current].ContainFunc(namespaceStrings, funcName);
            }

            return funcILIndexDict.ContainsKey(funcName);
        }

        public NamespaceTable GetChildNamespace(IEnumerable<string> namespaceStrings)
        {
            return GetChildNamespace(namespaceStrings.GetEnumerator());
        }

        private NamespaceTable GetChildNamespace(IEnumerator<string> namespaceStrings)
        {
            if (namespaceStrings.MoveNext())
            {
                return childrenNamespaceTableDict[namespaceStrings.Current].GetChildNamespace(namespaceStrings);
            }

            return this;
        }

        private NamespaceTable GetOrCreateChildNamespace(IEnumerator<string> namespaceStrings)
        {
            if (namespaceStrings.MoveNext())
            {
                if (childrenNamespaceTableDict.TryGetValue(namespaceStrings.Current, out NamespaceTable child))
                {
                    return child.GetOrCreateChildNamespace(namespaceStrings);
                }
                else
                {
                    var child2= new NamespaceTable();
                    childrenNamespaceTableDict[namespaceStrings.Current] = child2;
                    return child2.GetOrCreateChildNamespace(namespaceStrings);
                }
            }

            return this;
        }

        public void AddFuncILIndex(IEnumerable<string> namespaceStrings, IEnumerable<string> funcNames)
        {
            var child = GetOrCreateChildNamespace(namespaceStrings.GetEnumerator());

            foreach (var funcName in funcNames)
            {
                child.SetFuncILIndex(funcName, -1);
            }
        }

        public FuncILIndex GetFuncILIndex(IEnumerable<string> namespaceStrings, string funcName)
        {
            var child = GetOrCreateChildNamespace(namespaceStrings.GetEnumerator());
            return child.GetFuncILIndex(funcName);
        }

        public bool TryGetFuncILIndex(IEnumerable<string> namespaceStrings, string funcName, out FuncILIndex funcILIndex)
        {
            var child = GetOrCreateChildNamespace(namespaceStrings.GetEnumerator());
            return child.TryGetFuncILIndex(funcName, out funcILIndex);
        }

        public FuncILIndex SetFuncILIndex(string funcName, int index)
        {
            if (funcILIndexDict.TryGetValue(funcName, out FuncILIndex funcILIndex))
            {
                if (index != -1)
                    funcILIndex.Index = index;
                return funcILIndex;
            }
            else
            {
                var funcILIndex2 = new FuncILIndex { Index = index };
                funcILIndexDict[funcName] = funcILIndex2;
                return funcILIndex2;
            }
        }

        public FuncILIndex GetFuncILIndex(string funcName)
        {
            return funcILIndexDict[funcName];
        }

        public bool TryGetFuncILIndex(string funcName, out FuncILIndex funcILIndex)
        {
            return funcILIndexDict.TryGetValue(funcName, out funcILIndex);
        }
    }
}
