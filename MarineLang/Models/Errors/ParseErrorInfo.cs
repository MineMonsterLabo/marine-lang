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
        public RangePosition ErrorRangePosition{ get; }
        public ErrorKind ErrorKind { get; }
        public ErrorCode ErrorCode { get; }


        public string FullErrorMessage => $"{ErrorMessage} \n {ErrorRangePosition} \nerror code: {(int)ErrorCode}";

        public ParseErrorInfo(
            string prefixErrorMessage, 
            ErrorCode errorCode = ErrorCode.Unknown, 
            ErrorKind errorKind = default, 
            RangePosition errorRangePosition = default
        )
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorKind = errorKind;
            ErrorRangePosition = errorRangePosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(string prefixErrorMessage, ErrorKind errorKind = default, RangePosition errorRangePosition = default)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorKind = errorKind;
            ErrorRangePosition = errorRangePosition;
            ErrorCode = ErrorCode.Unknown;
        }

        public ParseErrorInfo(string prefixErrorMessage, ErrorCode errorCode, RangePosition errorRangePosition)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorKind = ErrorKind.None;
            ErrorRangePosition = errorRangePosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(ErrorKind errorKind, ErrorCode errorCode = ErrorCode.Unknown)
        {
            ErrorKind = errorKind;
            ErrorRangePosition = default;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo()
        {
            ErrorKind = ErrorKind.None;
            ErrorRangePosition = default;
            ErrorCode = ErrorCode.Unknown;
        }
    }
}
