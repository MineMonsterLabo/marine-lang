namespace MarineLang.Models.Errors
{
    public enum ErrorKind
    {
        InComplete,
        ForceError,
        None,
    }

    public class ParseErrorInfo
    {
        public string ErrorMessage { get; }
        public Position ErrorPosition { get; }
        public ErrorKind ErrorKind { get; }
        public ErrorCode ErrorCode { get; }


        public string FullErrorMessage => $"{ErrorMessage} \n {ErrorPosition} \nerror code: {(int)ErrorCode}";

        public ParseErrorInfo(string errorMessage, ErrorCode errorCode = ErrorCode.Unknown, ErrorKind errorKind = default, Position errorPosition = default)
        {
            ErrorMessage = errorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(string errorMessage, ErrorKind errorKind = default, Position errorPosition = default)
        {
            ErrorMessage = errorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
            ErrorCode = ErrorCode.Unknown;
        }

        public ParseErrorInfo(string errorMessage, ErrorCode errorCode, Position errorPosition)
        {
            ErrorMessage = errorMessage;
            ErrorKind = ErrorKind.None;
            ErrorPosition = errorPosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(ErrorKind errorKind, ErrorCode errorCode = ErrorCode.Unknown)
        {
            ErrorMessage = "";
            ErrorKind = errorKind;
            ErrorPosition = default;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo()
        {
            ErrorMessage = "";
            ErrorKind = ErrorKind.None;
            ErrorPosition = default;
            ErrorCode = ErrorCode.Unknown;
        }
    }
}
