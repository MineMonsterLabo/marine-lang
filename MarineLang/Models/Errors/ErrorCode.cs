using System;

namespace MarineLang.Models.Errors
{
    public enum ErrorCode
    {
        SyntaxNonEndWord,
        SyntaxNonFuncWord,
        SyntaxNonFuncName,
        SyntaxNonFuncParen,
        SyntaxNonRetExpr,
        SyntaxNonLetVarName,
        SyntaxNonLetEqual,
        SyntaxNonEqualExpr,
        RuntimeMemberNotFound,
        RuntimeMemberAccessPrivate,
        Unknown
    }

    public static class ErrorCodeExtension
    {
        public static string ToErrorMessage(this ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.SyntaxNonEndWord:
                    return "関数の終わりにendがありません";
                case ErrorCode.SyntaxNonFuncWord:
                    return "関数定義が間違っています";
                case ErrorCode.SyntaxNonFuncName:
                    return "関数定義に関数名がありません";
                case ErrorCode.SyntaxNonFuncParen:
                    return "関数定義には()が必要です";
                case ErrorCode.SyntaxNonRetExpr:
                    return "retの後には式が必要です";
                case ErrorCode.SyntaxNonLetVarName:
                    return "letの後には変数名が必要です";
                case ErrorCode.SyntaxNonLetEqual:
                    return "letに=がありません";
                case ErrorCode.SyntaxNonEqualExpr:
                    return "=の後に式がありません";
                case ErrorCode.RuntimeMemberNotFound:
                    return "メンバーが見つかりません。";
                case ErrorCode.RuntimeMemberAccessPrivate:
                    return "privateなメンバーにアクセスしようとしました";
                case ErrorCode.Unknown:
                    return "";
            }

            throw new NotImplementedException();
        }
    }
}