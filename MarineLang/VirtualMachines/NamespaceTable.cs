using System.Collections.Generic;
using System.IO;
using MarineLang.VirtualMachines.BinaryImage;

namespace MarineLang.VirtualMachines
{
    public class NamespaceTable
    {
        private readonly Dictionary<string, NamespaceTable> childrenNamespaceTableDict =
            new Dictionary<string, NamespaceTable>();

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
                    var child2 = new NamespaceTable();
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

        public bool TryGetFuncILIndex(IEnumerable<string> namespaceStrings, string funcName,
            out FuncILIndex funcILIndex)
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

        internal void ReadBinaryFormat(BinaryReader reader)
        {
            var list = new List<NamespaceTable>();
            var tableIndexesList = new List<KeyValuePair<string, int>[]>();
            var tableCount = reader.Read7BitEncodedIntPolyfill();
            for (int i = 0; i < tableCount; i++)
            {
                var tableIndexes = new List<KeyValuePair<string, int>>();
                var tableIndexCount = reader.Read7BitEncodedIntPolyfill();
                for (int j = 0; j < tableIndexCount; j++)
                {
                    var key = reader.ReadString();
                    var value = reader.Read7BitEncodedIntPolyfill();
                    tableIndexes.Add(new KeyValuePair<string, int>(key, value));
                }

                var namespaceTable = new NamespaceTable();
                var funcILIndexCount = reader.Read7BitEncodedIntPolyfill();
                for (int j = 0; j < funcILIndexCount; j++)
                {
                    var key = reader.ReadString();
                    var value = reader.Read7BitEncodedIntPolyfill();
                    namespaceTable.funcILIndexDict.Add(key, new FuncILIndex { Index = value });
                }

                tableIndexesList.Add(tableIndexes.ToArray());
                list.Add(namespaceTable);
            }

            var index = 0;
            foreach (var namespaceTable in list)
            {
                var tableIndexes = tableIndexesList[index++];
                foreach (var pair in tableIndexes)
                {
                    namespaceTable.childrenNamespaceTableDict.Add(pair.Key, list[pair.Value]);
                }
            }

            var root = list[0];
            childrenNamespaceTableDict.Clear();
            funcILIndexDict.Clear();

            foreach (var namespaceTable in root.childrenNamespaceTableDict)
            {
                childrenNamespaceTableDict.Add(namespaceTable.Key, namespaceTable.Value);
            }

            foreach (var funcIlIndex in root.funcILIndexDict)
            {
                funcILIndexDict.Add(funcIlIndex.Key, funcIlIndex.Value);
            }
        }

        internal void WriteBinaryFormat(BinaryWriter writer)
        {
            var list = new List<KeyValuePair<string, NamespaceTable>>();
            var queue = new Queue<KeyValuePair<string, NamespaceTable>>();
            queue.Enqueue(new KeyValuePair<string, NamespaceTable>(null, this));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var pair in current.Value.childrenNamespaceTableDict)
                {
                    queue.Enqueue(pair);
                }

                list.Add(current);
            }

            writer.Write7BitEncodedIntPolyfill(list.Count);
            foreach (var pair in list)
            {
                var tableDict = pair.Value.childrenNamespaceTableDict;
                writer.Write7BitEncodedIntPolyfill(tableDict.Count);
                foreach (var table in tableDict)
                {
                    var index = list.IndexOf(table);
                    writer.Write(table.Key);
                    writer.Write7BitEncodedIntPolyfill(index);
                }

                var funcILIndexDict = pair.Value.funcILIndexDict;
                writer.Write7BitEncodedIntPolyfill(funcILIndexDict.Count);
                foreach (var ilIndex in funcILIndexDict)
                {
                    writer.Write(ilIndex.Key);
                    writer.Write7BitEncodedIntPolyfill(ilIndex.Value.Index);
                }
            }
        }
    }
}