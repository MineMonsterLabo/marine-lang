using MarineLang.VirtualMachines;
using MarineLang.VirtualMachines.MarineILs;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MarineLang.Models.Errors
{
    [Serializable()]
    public class ILRuntimeErrorInfo
    {
        readonly string prefixErrorMessage = "";

        public IMarineIL MarineIL { get; }
        public string ErrorMessage => $"[{MarineIL.ToString()}]:{prefixErrorMessage} {ErrorCode.ToErrorMessage()} \nerror code: {(int)ErrorCode}";
        public ErrorCode ErrorCode { get; }

        public ILRuntimeErrorInfo(IMarineIL marineIL, string prefixErrorMessage, ErrorCode errorCode = ErrorCode.Unknown)
        {
            MarineIL = marineIL;
            this.prefixErrorMessage = prefixErrorMessage;
            ErrorCode = errorCode;
        }
    }

    [Serializable()]
    public class RuntimeErrorInfo
    {
        public ILRuntimeErrorInfo ILRuntimeErrorInfo { get; }
        public DebugContext[] DebugContexts { get; }

        public string ErrorMessage => ILRuntimeErrorInfo.ErrorMessage + "\n" + string.Join("\n", DebugContexts.Select(e => e.ToString()));

        public RuntimeErrorInfo(ILRuntimeErrorInfo ilRuntimeErrorInfo, DebugContext[] debugContexts)
        {
            ILRuntimeErrorInfo = ilRuntimeErrorInfo;
            DebugContexts = debugContexts;
        }
    }

    [Serializable()]
    public class MarineILRuntimeException : Exception
    {

        public ILRuntimeErrorInfo ILRuntimeErrorInfo { get; }

        public MarineILRuntimeException(ILRuntimeErrorInfo runtimeErrorInfo)
       : base(runtimeErrorInfo.ErrorMessage)
        {
            ILRuntimeErrorInfo = runtimeErrorInfo;
        }

        public MarineILRuntimeException(ILRuntimeErrorInfo runtimeErrorInfo, Exception innerException)
            : base(runtimeErrorInfo.ErrorMessage, innerException)
        {
            ILRuntimeErrorInfo = runtimeErrorInfo;
        }

        protected MarineILRuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
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
