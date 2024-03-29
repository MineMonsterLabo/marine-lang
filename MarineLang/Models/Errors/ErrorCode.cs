﻿using System;

namespace MarineLang.Models.Errors
{
    public enum ErrorCode
    {
        SyntaxNonExpectedFuncWord,
        SyntaxNonEndWord,
        SyntaxNonFuncWord,
        SyntaxNonFuncName,
        SyntaxNonFuncParen,
        SyntaxNonRetExpr,
        SyntaxNonYieldExpr,
        SyntaxNonLetVarName,
        SyntaxNonLetEqual,
        SyntaxNonEqualExpr,
        RuntimeMemberNotFound,
        RuntimeIndexerNotFound,
        RuntimeMemberAccessPrivate,
        RuntimeOperatorNotFound,
        Unknown
    }

    public static class ErrorCodeExtension
    {
        public static string ToErrorMessage(this ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ErrorCode.SyntaxNonExpectedFuncWord:
                    return "関数をendで終わらせてからfunを指定してください";
                case ErrorCode.SyntaxNonEndWord:
                    return "関数の終わりにendがありません";
                case ErrorCode.SyntaxNonFuncWord:
                    return "関数定義にはfunが必要です";
                case ErrorCode.SyntaxNonFuncName:
                    return "関数定義に関数名がありません";
                case ErrorCode.SyntaxNonFuncParen:
                    return "関数定義には()が必要です";
                case ErrorCode.SyntaxNonRetExpr:
                    return "retの後には式が必要です";
                case ErrorCode.SyntaxNonYieldExpr:
                    return "yieldの後には式が必要です";
                case ErrorCode.SyntaxNonLetVarName:
                    return "letの後には変数名が必要です";
                case ErrorCode.SyntaxNonLetEqual:
                    return "letに=がありません";
                case ErrorCode.SyntaxNonEqualExpr:
                    return "=の後に式がありません";
                case ErrorCode.RuntimeMemberNotFound:
                    return "メンバーが見つかりません。";
                case ErrorCode.RuntimeIndexerNotFound:
                    return "対応するインデクサーが見つかりません。";
                case ErrorCode.RuntimeMemberAccessPrivate:
                    return "privateなメンバーにアクセスしようとしました";
                case ErrorCode.RuntimeOperatorNotFound:
                    return "演算子が見つかりません";
                case ErrorCode.Unknown:
                    return "不明なエラー";
            }

            throw new NotImplementedException();
        }
    }
}