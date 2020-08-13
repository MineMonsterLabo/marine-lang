using MarineLang.Models;

namespace MarineLang.SyntaxAnalysis
{
    public enum ErrorKind
    {
        InComplete,
        ForceError,
        None,
    }

    public class Error
    {
        public string ErrorMessage { get; }
        public Position ErrorPosition { get; }
        public ErrorKind ErrorKind { get; }

        public string FullErrorMessage => $"{ErrorMessage} {ErrorPosition}";

        public Error(string errorMessage, ErrorKind errorKind = default, Position errorPosition = default)
        {
            ErrorMessage = errorMessage;
            ErrorKind = errorKind;
            ErrorPosition = errorPosition;
        }

        public Error(string errorMessage, Position errorPosition)
        {
            ErrorMessage = errorMessage;
            ErrorKind = ErrorKind.None;
            ErrorPosition = errorPosition;
        }

        public Error(ErrorKind errorKind)
        {
            ErrorMessage = "";
            ErrorKind = errorKind;
            ErrorPosition = default;
        }

        public Error()
        {
            ErrorMessage = "";
            ErrorKind = ErrorKind.None;
            ErrorPosition = default;
        }
    }
}
