using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines
{
    public interface IReadonlyCsharpFuncTable
    {
        /// <summary>
        /// 該当する登録済みのC#関数のMethodInfoを返す
        /// </summary>
        MethodInfo GetCsharpFunc(IEnumerable<string> namespaceStrings, string funcName);

        /// <summary>
        /// 該当する登録済みのC#関数のMethodInfoを返す
        /// </summary>
        MethodInfo GetCsharpFunc(string funcName);

        /// <summary>
        /// 該当する登録済みのC#関数があるか判定
        /// </summary>
        /// <param name="isRecursive"> ネストした名前空間も検索対象にするかのフラグ</param>
        bool ContainFunc(IEnumerable<string> namespaceStrings, string funcName, bool isRecursive);

        /// <summary>
        /// 該当する登録済みのC#関数があるか判定
        /// </summary>
        /// <param name="isRecursive"> ネストした名前空間も検索対象にするかのフラグ</param>
        bool ContainFunc(string funcName, bool isRecursive);

        /// <summary>
        /// 登録済みのC#関数一覧を返す
        /// </summary>
        IEnumerable<(string FuncName, MethodInfo MethodInfo)> GetFuncs(IEnumerable<string> namespaceStrings);

        /// <summary>
        /// 登録済みのC#関数一覧を返す
        /// </summary>
        IEnumerable<(string FuncName, MethodInfo MethodInfo)> GetFuncs();

        /// <summary>
        /// 登録済みの名前空間一覧を返す
        /// </summary>
        /// <param name="isRecursive"> ネストした名前空間も対象にするかのフラグ</param>
        IEnumerable<(string NameSpace, IReadonlyCsharpFuncTable Child)> GetNamespaceChildren(bool isRecursive);
    }

    class CsharpFuncTable: IReadonlyCsharpFuncTable
    {
        private readonly Dictionary<string, CsharpFuncTable> childrenNamespaceTableDict = new Dictionary<string, CsharpFuncTable>();
        private readonly Dictionary<string, MethodInfo> csharpFuncDict = new Dictionary<string, MethodInfo>();

        private CsharpFuncTable GetOrCreateChildNamespace(IEnumerator<string> namespaceStrings)
        {
            if (namespaceStrings.MoveNext())
            {
                if (childrenNamespaceTableDict.TryGetValue(namespaceStrings.Current, out CsharpFuncTable child))
                {
                    return child.GetOrCreateChildNamespace(namespaceStrings);
                }
                else
                {
                    var child2 = new CsharpFuncTable();
                    childrenNamespaceTableDict[namespaceStrings.Current] = child2;
                    return child2.GetOrCreateChildNamespace(namespaceStrings);
                }
            }

            return this;
        }

        public void AddCsharpFunc(IEnumerable<string> namespaceStrings, string funcName, MethodInfo methodInfo)
        {
            var child = GetOrCreateChildNamespace(namespaceStrings.GetEnumerator());
            child.AddCsharpFunc(funcName, methodInfo);
        }

        public void AddCsharpFunc(string funcName, MethodInfo methodInfo)
        {
            csharpFuncDict.Add(funcName, methodInfo);
        }

        public MethodInfo GetCsharpFunc(IEnumerable<string> namespaceStrings,string funcName)
        {
            var child = GetOrCreateChildNamespace(namespaceStrings.GetEnumerator());
            return child.GetCsharpFunc(funcName);
        }

        public MethodInfo GetCsharpFunc(string funcName)
        {
            return csharpFuncDict[funcName];
        }

        public bool ContainFunc(IEnumerable<string> namespaceStrings, string funcName, bool isRecursive)
        {
            return GetOrCreateChildNamespace(namespaceStrings.GetEnumerator()).ContainFunc(funcName, isRecursive);
        }

        public bool ContainFunc(string funcName, bool isRecursive)
        {
            if (isRecursive == false)
            {
                return csharpFuncDict.ContainsKey(funcName);
            }

            return
                csharpFuncDict.ContainsKey(funcName) ||
                GetNamespaceChildren(isRecursive: true).Any(_ => _.Child.ContainFunc(funcName, isRecursive: false));
        }

        public IEnumerable<(string FuncName, MethodInfo MethodInfo)> GetFuncs(IEnumerable<string> namespaceStrings)
        {
            return GetOrCreateChildNamespace(namespaceStrings.GetEnumerator()).GetFuncs();
        }

        public IEnumerable<(string FuncName, MethodInfo MethodInfo)> GetFuncs()
        {
            return csharpFuncDict.Select(pair => (pair.Key, pair.Value));
        }

        public IEnumerable<(string NameSpace, IReadonlyCsharpFuncTable Child)> GetNamespaceChildren(bool isRecursive)
        {
            if (isRecursive == false)
            {
                return childrenNamespaceTableDict.Select(pair => (pair.Key, (IReadonlyCsharpFuncTable)pair.Value));
            }

            return
                childrenNamespaceTableDict
                .SelectMany(
                    pair =>
                        pair.Value.GetNamespaceChildren(isRecursive: true)
                        .Append((pair.Key, pair.Value))
                );
        }
    }
}
