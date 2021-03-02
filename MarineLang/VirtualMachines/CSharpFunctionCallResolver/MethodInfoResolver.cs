using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines.CSharpFunctionCallResolver
{
    class MethodInfoResolver
    {
        struct Data
        {
            public MethodInfo methodInfo;
            public IEnumerable<TypeDistance> paramsTypeDistances;
        }

        static public MethodInfo Select(Type classType, string funcName, BindingFlags bindingFlags, Type[] types)
        {
            var typesLength = types.Length;
            if (classType == null)
                return null;
            var methodInfos =
                classType.GetMethods(bindingFlags)
                    .Where(MatchMethodInfo(funcName,typesLength));

            var dataList = methodInfos.Select(
                m => {
                    var parameters = m.GetParameters();
                    Data data;
                    data.methodInfo = m;
                    data.paramsTypeDistances =
                        parameters.
                            Zip(types, (param, type) =>
                                TypeDistanceGenerator.Generate(type, param.ParameterType));

                    return data;
                }).Where(tuple => !tuple.paramsTypeDistances.Contains(null));

            var nearestData = new Data { paramsTypeDistances = null };

            foreach (var data in dataList)
            {

                if (nearestData.paramsTypeDistances == null)
                {
                    nearestData = data;
                    continue;
                }

                var sum = data.paramsTypeDistances.Zip(nearestData.paramsTypeDistances,
                    (paramsPriority, tParamsPriority) => paramsPriority.CompareTo(tParamsPriority))
                    .Sum();

                if (sum == 0)
                {
                    throw new Exception("比較不可能");
                }
                if (sum >0)
                {
                    nearestData = data;
                }
            }
            if (nearestData.methodInfo == null)
                return null;
            if (nearestData.methodInfo.IsGenericMethod)
            {
                throw new NotImplementedException("実装めんどくさい");
            }
            return nearestData.methodInfo;
        }

        static Func<MethodInfo, bool> MatchMethodInfo(string funcName, int typeLength)
        {
            return (methodInfo) =>
            {
                if (methodInfo.Name != funcName)
                    return false;

                var parameterInfoArray = methodInfo.GetParameters();
                var maxLength = parameterInfoArray.Length;
                var minLength = parameterInfoArray.Where(x => x.IsOptional == false).Count();
                return minLength <= typeLength && typeLength <= maxLength;
            };
        }
    }
}
