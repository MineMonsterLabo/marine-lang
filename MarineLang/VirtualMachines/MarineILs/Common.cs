using MarineLang.Models.Errors;

namespace MarineLang.VirtualMachines.MarineILs
{
    public interface IMarineIL
    {
        void Run(LowLevelVirtualMachine vm);
    }

    public static class IMarineILExtension
    {
        public static void ThrowRuntimeError(this IMarineIL marineIL, string errorMessage, ErrorCode errorCode)
        {
            throw new MarineILRuntimeException(
                  new ILRuntimeErrorInfo(
                      marineIL,
                      errorMessage,
                      errorCode
                  )
              );
        }
    }
}