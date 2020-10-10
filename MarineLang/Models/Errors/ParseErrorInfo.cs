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
        string prefixErrorMessage = "";

        public string ErrorMessage => prefixErrorMessage + ErrorCode.ToErrorMessage();
        public Position ErrorPosition { get; }
        public ErrorKind ErrorKind { get; }
        public ErrorCode ErrorCode { get; }


        public string FullErrorMessage => $"{ErrorMessage} \n {ErrorPosition} \nerror code: {(int)ErrorCode}";

        public ParseErrorInfo(string prefixErrorMessage, ErrorCode errorCode = ErrorCode.Unknown, ErrorKind errorKind = default, Position errorPosition = default)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(string prefixErrorMessage, ErrorKind errorKind = default, Position errorPosition = default)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
            ErrorCode = ErrorCode.Unknown;
        }

        public ParseErrorInfo(string prefixErrorMessage, ErrorCode errorCode, Position errorPosition)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorKind = ErrorKind.None;
            ErrorPosition = errorPosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(ErrorKind errorKind, ErrorCode errorCode = ErrorCode.Unknown)
        {
            ErrorKind = errorKind;
            ErrorPosition = default;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo()
        {
            ErrorKind = ErrorKind.None;
            ErrorPosition = default;
            ErrorCode = ErrorCode.Unknown;
        }
    }
}
