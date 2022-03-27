using System;

namespace MarineLang.Models.Errors
{
    public class ParseErrorInfo
    {
        string prefixErrorMessage = "";

        public string ErrorMessage => prefixErrorMessage + ErrorCode.ToErrorMessage();
        public RangePosition ErrorRangePosition { get; }
        public ErrorCode ErrorCode { get; }


        public string FullErrorMessage => $"{ErrorMessage} {Environment.NewLine} {ErrorRangePosition} {Environment.NewLine}error code: {(int)ErrorCode}";

        public ParseErrorInfo(
            string prefixErrorMessage,
            RangePosition errorRangePosition,
            ErrorCode errorCode = ErrorCode.Unknown
        )
        {
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorRangePosition = errorRangePosition;
            ErrorCode = errorCode;
        }
    }
}
