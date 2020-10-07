using MarineLang.Models.Errors;
using System;

namespace MarineLang.SyntaxAnalysis
{

    public interface IParseResult<out T>
    {
        bool IsError { get; }
        ParseErrorInfo Error { get; }

        T Value { get; }

        IParseResult<TT> Map<TT>(Func<T, TT> func);

        IParseResult<TT> CastError<TT>();
    }

    public class ParseResult<T> : IParseResult<T>
    {
        public bool IsError => Error != null;
        public ParseErrorInfo Error { get; }
        public T Value { get; }

        public ParseResult(ParseErrorInfo error, T value)
        {
            Value = value;
            Error = error;
        }

        public IParseResult<TT> Map<TT>(Func<T, TT> func)
        {
            if (IsError)
                return ParseResult<TT>.CreateError(Error);
            return ParseResult<TT>.CreateSuccess(func(Value));
        }

        public IParseResult<TT> CastError<TT>()
        {
            return ParseResult<TT>.CreateError(Error);
        }

        public static IParseResult<T> CreateSuccess(T value)
        {
            return new ParseResult<T>(null, value);
        }

        public static IParseResult<T> CreateError(ParseErrorInfo error)
        {
            return new ParseResult<T>(error, default);
        }
    }
}
