using MarineLang.VirtualMachines.MarineILs;
using System;
using System.Runtime.Serialization;

namespace MarineLang.Models.Errors
{
    [Serializable()]
    public class RuntimeErrorInfo
    {
        readonly string prefixErrorMessage = "";

        public readonly IMarineIL marineIL;
        public string ErrorMessage => $"[{marineIL.ToString()}]:{prefixErrorMessage} {ErrorCode.ToErrorMessage()} \nerror code: {(int)ErrorCode}";
        public ErrorCode ErrorCode { get; }

        public RuntimeErrorInfo(IMarineIL marineIL, string prefixErrorMessage, ErrorCode errorCode = ErrorCode.Unknown)
        {
            this.marineIL = marineIL;
            this.prefixErrorMessage = prefixErrorMessage;
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
