using MarineLang.Models;
using MarineLang.Models.Errors;

namespace MarineLang.VirtualMachines.MarineILs
{
    public class ILDebugInfo
    {
        public readonly Position position;

        public ILDebugInfo(Position position)
        {
            this.position = position;
        }
    }

    public interface IMarineIL
    {
        ILDebugInfo ILDebugInfo { get; }
        void Run(LowLevelVirtualMachine vm);
    }

    public static class IMarineILExtension
    {
        public static void ThrowRuntimeError(this IMarineIL marineIL,  string errorMessage,ErrorCode errorCode)
        {
            throw new MarineRuntimeException(
                  new RuntimeErrorInfo(
                      errorMessage,
                      errorCode,
                      marineIL.ILDebugInfo?.position
                  )
              );
        }
    }
}