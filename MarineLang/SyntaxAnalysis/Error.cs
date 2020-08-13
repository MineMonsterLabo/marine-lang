using MarineLang.Models;

namespace MarineLang.SyntaxAnalysis
{
    public enum ErrorKind
    {
        InComplete,
        ForceError,
        None,
    }

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

    public class Error
    {
        public string ErrorMessage { get; }
        public Position ErrorPosition { get; }
        public ErrorKind ErrorKind { get; }
        public ErrorCode ErrorCode { get; }


        public string FullErrorMessage => $"{ErrorMessage} \n {ErrorPosition} \nerror code: {(int)ErrorCode}";

        public Error(string errorMessage, ErrorCode errorCode = ErrorCode.Unknown, ErrorKind errorKind = default, Position errorPosition = default)
        {
            ErrorMessage = errorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
            ErrorCode = errorCode;
        }

        public Error(string errorMessage, ErrorKind errorKind = default, Position errorPosition = default)
        {
            ErrorMessage = errorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
            ErrorCode = ErrorCode.Unknown;
        }

        public Error(string errorMessage, ErrorCode errorCode, Position errorPosition)
        {
            ErrorMessage = errorMessage;
            ErrorKind = ErrorKind.None;
            ErrorPosition = errorPosition;
            ErrorCode = errorCode;
        }

        public Error(ErrorKind errorKind, ErrorCode errorCode = ErrorCode.Unknown)
        {
            ErrorMessage = "";
            ErrorKind = errorKind;
            ErrorPosition = default;
            ErrorCode = errorCode;
        }

        public Error()
        {
            ErrorMessage = "";
            ErrorKind = ErrorKind.None;
            ErrorPosition = default;
            ErrorCode = ErrorCode.Unknown;
        }
    }
}
