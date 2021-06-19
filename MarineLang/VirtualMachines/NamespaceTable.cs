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

        public NamespaceTable GetOrCreateChildNamespace(IEnumerator<string> namespaceStrings)
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

        public FuncILIndex AddFuncILIndex(IEnumerable<string> namespaceStrings, string funcName)
        {
            var child = GetOrCreateChildNamespace(namespaceStrings.GetEnumerator());
            return child.SetFuncILIndex(funcName, -1);
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

        public FuncILIndex GetFuncILIndex(IEnumerable<string> namespaceStrings, string funcName)
        {
            return GetFuncILIndex(namespaceStrings.GetEnumerator(), funcName);
        }

        public FuncILIndex GetFuncILIndex(IEnumerator<string> namespaceStrings, string funcName)
        {
            if (namespaceStrings.MoveNext())
            {
                return childrenNamespaceTableDict[namespaceStrings.Current].GetFuncILIndex(namespaceStrings, funcName);
            }

            return funcILIndexDict[funcName];
        }
    }
}
