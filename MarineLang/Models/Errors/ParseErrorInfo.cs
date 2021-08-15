namespace MarineLang.Models.Errors
{
    public class ParseErrorInfo
    {
        string prefixErrorMessage = "";

        public string ErrorMessage => prefixErrorMessage + ErrorCode.ToErrorMessage();
        public RangePosition ErrorRangePosition{ get; }
        public ErrorCode ErrorCode { get; }


        public string FullErrorMessage => $"{ErrorMessage} \n {ErrorRangePosition} \nerror code: {(int)ErrorCode}";

        public ParseErrorInfo(
            string prefixErrorMessage, 
            ErrorCode errorCode = ErrorCode.Unknown, 
            RangePosition errorRangePosition = default
        )
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorRangePosition = errorRangePosition;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo(string prefixErrorMessage, RangePosition errorRangePosition = default)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorRangePosition = errorRangePosition;
            ErrorCode = ErrorCode.Unknown;
        }

        public ParseErrorInfo(ErrorCode errorCode = ErrorCode.Unknown)
        {
            ErrorRangePosition = default;
            ErrorCode = errorCode;
        }

        public ParseErrorInfo()
        {
            ErrorRangePosition = default;
            ErrorCode = ErrorCode.Unknown;
        }
    }
}
