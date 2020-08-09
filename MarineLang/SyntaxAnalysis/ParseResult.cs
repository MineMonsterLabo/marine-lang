using System;

namespace MarineLang.SyntaxAnalysis
{

    public interface IParseResult<out T>
    {
        bool IsError { get; }
        string ErrorMessage { get; }
        T Value { get; }

        IParseResult<TT> Map<TT>(Func<T, TT> func);

        IParseResult<TT> CastError<TT>();
    }
    public struct ParseResult<T> : IParseResult<T>
    {
        public bool IsError { get; }
        public string ErrorMessage { get; }
        public T Value { get; }

        public ParseResult(bool isError, string errorMessage, T value)
        {
            IsError = isError;
            ErrorMessage = errorMessage;
            Value = value;
        }

        public IParseResult<TT> Map<TT>(Func<T, TT> func)
        {
            if (IsError)
                return ParseResult<TT>.Error(ErrorMessage);
            return ParseResult<TT>.Success(func(Value));
        }

        public IParseResult<TT> CastError<TT>()
        {
            return ParseResult<TT>.Error(ErrorMessage);
        }

        public static IParseResult<T> Success(T value)
        {
            return new ParseResult<T>(false, "", value);
        }

        public static IParseResult<T> Error(string errorMessage)
        {
            return new ParseResult<T>(true, errorMessage, default);
        }
    }
}
