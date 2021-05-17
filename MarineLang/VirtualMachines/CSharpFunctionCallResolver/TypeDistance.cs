using System;

namespace MarineLang.VirtualMachines.CSharpFunctionCallResolver
{
    public class TypeDistance : IComparable<TypeDistance>
    {
        public enum MatchKind
        {
            //型一致
            TypeMatch = 0,
            //Null許容型一致
            NullableTypeMatch = 0,
            //ジェネリック型一致
            GenericTypeMatch,
            //暗黙的キャストによる一致
            ImplicitCastMatch,
            //アップキャストによる一致
            UpCastMatch,
            //ジェネリック型のアップキャストによる一致
            GenericTypeUpCastMatch,
            //オブジェクト型への一致
            ObjectMatch,
        }
        public MatchKind matchkind { get; set; }
        //アップキャストの深度
        public int upCastNest = 0;

        //型の具体性
        public int concreteness = 0;

        //暗黙的キャストの優先順位
        public int implicitCastPriority = 0;

        //優先順位比較
        //0で優先順位が等しい,もしくは判別不能
        //1でxのほうが優先順位が高い
        //-1でyのほうが優先順位が高い

        public int CompareTo(TypeDistance y)
        {
            if (matchkind < y.matchkind)
                return 1;
            else if (matchkind > y.matchkind)
                return -1;
            switch (matchkind)
            {

                case MatchKind.TypeMatch:
                    return 0;

                case MatchKind.GenericTypeMatch:
                    if (concreteness > y.concreteness)
                        return 1;
                    else if (concreteness < y.concreteness)
                        return -1;
                    return 0;

                case MatchKind.ImplicitCastMatch:
                    if (implicitCastPriority > y.implicitCastPriority)
                        return 1;
                    else if (implicitCastPriority < y.implicitCastPriority)
                        return -1;
                    return 0;

                case MatchKind.UpCastMatch:
                    if (upCastNest < y.upCastNest)
                        return 1;
                    else if (upCastNest > y.upCastNest)
                        return -1;
                    return 0;
                case MatchKind.ObjectMatch:
                    return 0;
            }

            throw new Exception("error!");
        }
    }
}
