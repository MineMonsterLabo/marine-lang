using System;

namespace MarineLang
{
    public struct ParseResult<T>
    {
        public readonly bool isError;
        public readonly string errorMessage;
        public readonly T value;

        public ParseResult(bool isError, string errorMessage, T value)
        {
            this.isError = isError;
            this.errorMessage = errorMessage;
            this.value = value;
        }

        public ParseResult<TT> Map<TT>(Func<T, TT> func)
        {
            if (isError)
                return ParseResult<TT>.Error(errorMessage);
            return ParseResult<TT>.Success(func(value));
        }

        public ParseResult<TT> CastError<TT>()
        {
            return ParseResult<TT>.Error(errorMessage);
        }

        public static ParseResult<T> Success(T ast)
        {
            return new ParseResult<T>(false, "", ast);
        }

        public static ParseResult<T> Error(string errorMessage)
        {
            return new ParseResult<T>(true, errorMessage, default);
        }
    }
}
