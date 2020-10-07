using System;

namespace MarineLang.Models.Errors
{
    public enum ErrorCode
    {
        NonEndWord,
        NonFuncWord,
        NonFuncName,
        NonFuncParen,
        NonRetExpr,
        NonLetVarName,
        NonLetEqual,
        NonEqualExpr,
        Unknown
    }

    public static class ErrorCodeExtension
    {
        public static string ToErrorMessage(this ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.NonEndWord:
                    return "関数の終わりにendがありません";
                case ErrorCode.NonFuncWord:
                    return "関数定義が間違っています";
                case ErrorCode.NonFuncName:
                    return "関数定義に関数名がありません";
                case ErrorCode.NonFuncParen:
                    return "関数定義には()が必要です";
                case ErrorCode.NonRetExpr:
                    return "retの後には式が必要です";
                case ErrorCode.NonLetVarName:
                    return "letの後には変数名が必要です";
                case ErrorCode.NonLetEqual:
                    return "letに=がありません";
                case ErrorCode.NonEqualExpr:
                    return "=の後に式がありません";
                case ErrorCode.Unknown:
                    return "";
            }
            throw new NotImplementedException();
        }
    }
}
