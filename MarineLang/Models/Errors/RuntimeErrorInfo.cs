using System;
using System.Runtime.Serialization;

namespace MarineLang.Models.Errors
{
    [Serializable()]
    public class RuntimeErrorInfo
    {
        readonly string prefixErrorMessage = "";

        public readonly Position? errorPosition;

        public string ErrorMessage => prefixErrorMessage + ErrorCode.ToErrorMessage() + $" \n {errorPosition} \nerror code: {(int)ErrorCode}";
        public ErrorCode ErrorCode { get; }

        public RuntimeErrorInfo(string prefixErrorMessage, ErrorCode errorCode = ErrorCode.Unknown, Position? errorPosition = null)
        {
            this.prefixErrorMessage = prefixErrorMessage;
            this.errorPosition = errorPosition;
            ErrorCode = errorCode;
        }
    }

    [Serializable()]
    public class MarineRuntimeException : Exception
    {

        public RuntimeErrorInfo RuntimeErrorInfo { get; }

        public MarineRuntimeException(RuntimeErrorInfo runtimeErrorInfo)
       : base(runtimeErrorInfo.ErrorMessage)
        {
            RuntimeErrorInfo = runtimeErrorInfo;
        }

        public MarineRuntimeException(RuntimeErrorInfo runtimeErrorInfo, Exception innerException)
            : base(runtimeErrorInfo.ErrorMessage, innerException)
        {
            RuntimeErrorInfo = runtimeErrorInfo;
        }

        protected MarineRuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
