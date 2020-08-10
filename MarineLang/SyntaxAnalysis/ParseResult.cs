using MarineLang.Models;
using System;

namespace MarineLang.SyntaxAnalysis
{

    public interface IParseResult<out T>
    {
        bool IsError { get; }
        string ErrorMessage { get; }
        string FullErrorMessage { get; }

        T Value { get; }

        IParseResult<TT> Map<TT>(Func<T, TT> func);

        IParseResult<TT> CastError<TT>();
    }
    public class ParseResult<T> : IParseResult<T>
    {
        public bool IsError { get; }
        public string ErrorMessage { get; }
        public string FullErrorMessage => $"{ErrorMessage} {ErrorPosition}";
        public Position ErrorPosition { get; }
        public T Value { get; }

        public ParseResult(bool isError, string errorMessage, Position errorPosition, T value)
        {
            IsError = isError;
            ErrorMessage = errorMessage;
            ErrorPosition = errorPosition;
            Value = value;
        }

        public IParseResult<TT> Map<TT>(Func<T, TT> func)
        {
            if (IsError)
                return ParseResult<TT>.Error(ErrorMessage, ErrorPosition);
            return ParseResult<TT>.Success(func(Value));
        }

        public IParseResult<TT> CastError<TT>()
        {
            return ParseResult<TT>.Error(ErrorMessage, ErrorPosition);
        }

        public static IParseResult<T> Success(T value)
        {
            return new ParseResult<T>(false, "", default, value);
        }

        public static IParseResult<T> Error(string errorMessage, Position errorPosition = default)
        {
            return new ParseResult<T>(true, errorMessage, errorPosition, default);
        }
    }
}
