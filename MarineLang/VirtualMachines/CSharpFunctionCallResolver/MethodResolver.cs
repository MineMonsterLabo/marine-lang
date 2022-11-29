using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines.CSharpFunctionCallResolver
{
    class MethodResolver
    {
        struct Data<T>where T:MethodBase
        {
            public T methodBase;
            public IEnumerable<TypeDistance> paramsTypeDistances;
        }

        static public T Select<T>(IEnumerable<T> inputMethodBases, Type[] argTypes, Type[] genericTypes = null)
        where T : MethodBase
        {
            genericTypes = genericTypes ?? new Type[] { };
            var methodBases = inputMethodBases.Where(MatchMethodBase<T>(argTypes.Length, genericTypes.Length));

            var dataList = methodBases.Select(
                methodBase =>
                {
                    var parameters = methodBase.GetParameters();
                    Data<T> data;
                    data.methodBase = methodBase;
                    data.paramsTypeDistances =
                        parameters.Zip(argTypes, (param, type) =>
                            TypeDistanceGenerator.Generate(type, param.ParameterType));

                    return data;
                }).Where(tuple => !tuple.paramsTypeDistances.Contains(null));

            var nearestData = new Data<T> { paramsTypeDistances = null };

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

            return nearestData.methodBase;
        }

        public static MethodInfo ResolveGenericMethod(MethodInfo methodInfo, Type[] argTypes, Type[] genericTypes = null)
        {
            if (methodInfo.IsGenericMethod)
            {
                if (genericTypes != null && genericTypes.Length > 0)
                {
                    return methodInfo.MakeGenericMethod(genericTypes);
                }

                genericTypes = methodInfo.GetGenericArguments();
                var genericParamsInfos = methodInfo.GetParameters()
                    .Select((param, index) => (ParamType: param.ParameterType, Index: index))
                    .Where(info => info.ParamType.IsGenericParameter)
                    .Select(info => (GenericPos: info.ParamType.GenericParameterPosition, ParamIndex: info.Index));

                foreach (var genericParamsInfo in genericParamsInfos)
                {
                    genericTypes[genericParamsInfo.GenericPos] = argTypes[genericParamsInfo.ParamIndex];
                }

                return methodInfo.MakeGenericMethod(genericTypes);
            }
            return methodInfo;
        }

        static Func<T, bool> MatchMethodBase<T>(int argTypeLength,int genericTypeLength) where T : MethodBase
        {
            return methodBase =>
            {
                if (genericTypeLength > 0 && methodBase.GetGenericArguments().Length != genericTypeLength)
                {
                    return false;
                }

                var parameterInfoArray = methodBase.GetParameters();
                var maxLength = parameterInfoArray.Length;
                var minLength = parameterInfoArray.Count(x => x.IsOptional == false);
                return minLength <= argTypeLength && argTypeLength <= maxLength;
            };
        }
    }
}