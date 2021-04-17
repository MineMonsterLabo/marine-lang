using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines.CSharpFunctionCallResolver
{
    class MethodBaseResolver
    {
        struct Data
        {
            public MethodBase methodBase;
            public IEnumerable<TypeDistance> paramsTypeDistances;
        }

        static public MethodBase Select(IEnumerable<MethodBase> inputMethodBases, Type[] types)
        {
            var typesLength = types.Length;
            var methodBases = inputMethodBases.Where(MatchMethodBase(typesLength));

            var dataList = methodBases.Select(
                m =>
                {
                    var parameters = m.GetParameters();
                    Data data;
                    data.methodBase = m;
                    data.paramsTypeDistances =
                        parameters.Zip(types, (param, type) =>
                            TypeDistanceGenerator.Generate(type, param.ParameterType));

                    return data;
                }).Where(tuple => !tuple.paramsTypeDistances.Contains(null));

            var nearestData = new Data {paramsTypeDistances = null};

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

                if (sum > 0)
                {
                    nearestData = data;
                }
            }

            if (nearestData.methodBase == null)
                return null;
            if (nearestData.methodBase.IsGenericMethod)
            {
                throw new NotImplementedException("実装めんどくさい");
            }

            return nearestData.methodBase;
        }

        static Func<MethodBase, bool> MatchMethodBase(int typeLength)
        {
            return methodBase =>
            {
                var parameterInfoArray = methodBase.GetParameters();
                var maxLength = parameterInfoArray.Length;
                var minLength = parameterInfoArray.Count(x => x.IsOptional == false);
                return minLength <= typeLength && typeLength <= maxLength;
            };
        }
    }
}