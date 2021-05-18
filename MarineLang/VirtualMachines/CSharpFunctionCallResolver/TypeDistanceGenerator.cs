using System;

namespace MarineLang.VirtualMachines.CSharpFunctionCallResolver
{
    public static class TypeDistanceGenerator
    {
        static public TypeDistance Generate(Type t1, Type t2)
        {
            if (IsComplete(t1, t2))
                return new TypeDistance
                {
                    matchkind = TypeDistance.MatchKind.TypeMatch
                };

            if (IsNullableComplete(t1, t2))
                return new TypeDistance
                {
                    matchkind = TypeDistance.MatchKind.NullableTypeMatch
                };

            var implicitCastPriority = IsPrimitiveImplicitCast(t1, t2);
            if (implicitCastPriority.HasValue)
                return new TypeDistance
                {
                    implicitCastPriority = implicitCastPriority.Value,
                    matchkind = TypeDistance.MatchKind.ImplicitCastMatch
                };

            var upCastNest = IsUpCast(t1, t2);
            if (upCastNest.HasValue)
                return new TypeDistance
                {
                    upCastNest = upCastNest.Value,
                    matchkind = TypeDistance.MatchKind.UpCastMatch
                };

            var concreteness = IsGeneric(t1, t2);
            if (concreteness.HasValue)
                return new TypeDistance
                {
                    matchkind = TypeDistance.MatchKind.GenericTypeMatch,
                    concreteness = concreteness.Value
                };

            var genericUpCastInfo = GetGenericUpCastInfo(t1, t2);
            if (genericUpCastInfo != null)
                return new TypeDistance
                {
                    upCastNest = genericUpCastInfo.Value.upCastNest,
                    matchkind = TypeDistance.MatchKind.GenericTypeUpCastMatch
                };

            if (t2 == typeof(object))
                return new TypeDistance
                {
                    matchkind = TypeDistance.MatchKind.ObjectMatch
                };

            return null;
        }

        //型が等しかったらtrue
        static bool IsComplete(Type t1, Type t2)
        {
            return t1 == t2;
        }

        //Nullable内部の型が等しかったらtrue
        static bool IsNullableComplete(Type t1, Type t2)
        {
            Type underlyingType = Nullable.GetUnderlyingType(t2);
            if (underlyingType != null)
                return t1 == underlyingType;

            return false;
        }

        //アップキャストができるなら親の最小の深さを返す
        //ちがうなら-1
        static int? IsUpCast(Type t1, Type t2)
        {
            if (t1.BaseType != null)
            {
                if (t1.BaseType == t2)
                    return 1;
                var upCastNest = IsUpCast(t1.BaseType, t2);
                if (upCastNest.HasValue)
                    return upCastNest + 1;
            }

            foreach (var interfaceType in t1.GetInterfaces())
            {
                if (interfaceType == t2)
                    return 1;
                var upCastNest = IsUpCast(interfaceType, t2);
                if (upCastNest.HasValue)
                    return upCastNest + 1;
            }

            return null;
        }

        //ジェネリックの判定
        //型の具体性を返す
        static int? IsGeneric(Type t1, Type t2)
        {
            if (t2.IsGenericParameter)
                return 1;

            if (t1 == t2)
                return 2;

            if (t1.IsArray && t2.IsArray && t1.GetArrayRank() == t2.GetArrayRank())
            {
                var hoge = IsGeneric(t1.GetElementType(), t2.GetElementType());
                if (hoge.HasValue==false)
                    return null;
                return hoge + 1;
            }

            if (t1.IsGenericType && t2.IsGenericType && t1.GetGenericTypeDefinition() == t2.GetGenericTypeDefinition())
            {
                var genericParams1 = t1.GetGenericArguments();
                var genericParams2 = t2.GetGenericArguments();
                if (genericParams1.Length != genericParams2.Length)
                    return null;

                int sum = 1;
                for (int i = 0; i < genericParams1.Length; i++)
                {
                    var hoge = IsGeneric(genericParams1[i], genericParams2[i]);
                    if (hoge.HasValue==false)
                        return null;
                    sum += hoge.Value;
                }

                return sum;
            }

            return null;
        }

        struct GenericUpCastInfo
        {
            public int upCastNest;
            public Type upCastedType;
        }

        //ジェネリック型を含むアップキャストの判定
        static GenericUpCastInfo? GetGenericUpCastInfo(Type t1, Type t2)
        {
            if (t2.IsGenericType == false)
                return null;

            if (t1.BaseType != null)
            {
                if (t1.BaseType.IsGenericType && t1.BaseType.GetGenericTypeDefinition() == t2.GetGenericTypeDefinition())
                    return
                        new GenericUpCastInfo
                        { upCastNest = 1, upCastedType = t1.BaseType };
                var genericUpCastInfo = GetGenericUpCastInfo(t1.BaseType, t2);
                if (genericUpCastInfo != null)
                    return new GenericUpCastInfo
                    { upCastNest = genericUpCastInfo.Value.upCastNest + 1, upCastedType = genericUpCastInfo.Value.upCastedType };
            }

            foreach (var interfaceType in t1.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == t2.GetGenericTypeDefinition())
                    return
                       new GenericUpCastInfo
                       { upCastNest = 1, upCastedType = interfaceType };
                var genericUpCastInfo = GetGenericUpCastInfo(interfaceType, t2);
                if (genericUpCastInfo != null)
                    return new GenericUpCastInfo
                    { upCastNest = genericUpCastInfo.Value.upCastNest + 1, upCastedType = genericUpCastInfo.Value.upCastedType };
            }

            return null;
        }

        //暗黙的型変換ができるならオーバーロードした際の優先順位を返す
        static int? IsPrimitiveImplicitCast(Type t1, Type t2)
        {
            if (t1.IsPrimitive && t2.IsPrimitive)
            {
                TypeCode t1code = Type.GetTypeCode(t1);
                TypeCode t2code = Type.GetTypeCode(t2);

                switch (t1code)
                {
                    case TypeCode.Char:
                        switch (t2code)
                        {
                            case TypeCode.UInt16: return 15;
                            case TypeCode.UInt32: return 13;
                            case TypeCode.Int32: return 14;
                            case TypeCode.UInt64: return 11;
                            case TypeCode.Int64: return 12;
                            case TypeCode.Single: return 10;
                            case TypeCode.Double: return 9;
                            default: return null;
                        }

                    case TypeCode.Byte:
                        switch (t2code)
                        {
                            case TypeCode.UInt16: return 14;
                            case TypeCode.Int16: return 15;
                            case TypeCode.UInt32: return 12;
                            case TypeCode.Int32: return 13;
                            case TypeCode.UInt64: return 10;
                            case TypeCode.Int64: return 11;
                            case TypeCode.Single: return 9;
                            case TypeCode.Double: return 8;
                            default: return null;
                        }

                    case TypeCode.SByte:
                        switch (t2code)
                        {
                            case TypeCode.Int16: return 15;
                            case TypeCode.Int32: return 14;
                            case TypeCode.Int64: return 13;
                            case TypeCode.Single: return 12;
                            case TypeCode.Double: return 11;
                            default: return null;
                        }

                    case TypeCode.UInt16:
                        switch (t2code)
                        {
                            case TypeCode.UInt32: return 14;
                            case TypeCode.Int32: return 15;
                            case TypeCode.UInt64: return 12;
                            case TypeCode.Int64: return 13;
                            case TypeCode.Single: return 11;
                            case TypeCode.Double: return 10;
                            default: return null;
                        }

                    case TypeCode.Int16:
                        switch (t2code)
                        {
                            case TypeCode.Int32: return 15;
                            case TypeCode.Int64: return 14;
                            case TypeCode.Single: return 13;
                            case TypeCode.Double: return 12;
                            default: return null;
                        }

                    case TypeCode.UInt32:
                        switch (t2code)
                        {
                            case TypeCode.UInt64: return 14;
                            case TypeCode.Int64: return 15;
                            case TypeCode.Single: return 13;
                            case TypeCode.Double: return 12;
                            default: return null;
                        }

                    case TypeCode.Int32:
                        switch (t2code)
                        {
                            case TypeCode.Int16: return 15;
                            case TypeCode.UInt16: return 14;
                            case TypeCode.UInt32: return 13;
                            case TypeCode.Int64: return 12;
                            case TypeCode.UInt64: return 11;
                            case TypeCode.Single: return 10;
                            case TypeCode.Double: return 9;
                            default: return null;
                        }

                    case TypeCode.UInt64:
                        switch (t2code)
                        {
                            case TypeCode.Single: return 15;
                            case TypeCode.Double: return 14;
                            default: return null;
                        }

                    case TypeCode.Int64:
                        switch (t2code)
                        {
                            case TypeCode.UInt64: return 15;
                            case TypeCode.Single: return 14;
                            case TypeCode.Double: return 13;
                            default: return null;
                        }

                    case TypeCode.Single:
                        switch (t2code)
                        {
                            case TypeCode.Double: return 15;
                            default: return null;
                        }
                }
            }

            return null;
        }
    }
}