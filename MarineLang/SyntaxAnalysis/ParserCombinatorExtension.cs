using MarineLang.Models;
using MarineLang.Streams;
using System;

namespace MarineLang.SyntaxAnalysis
{
    public static class ParserCombinatorExtension
    {
        public static Func<TokenStream, IParseResult<T>> InCompleteError<T>
            (this Func<TokenStream, IParseResult<T>> parser, Func<TokenStream, Error> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError && result.Error.ErrorKind == ErrorKind.InComplete)
                    return ParseResult<T>.CreateError(func(stream));
                return result;
            };
        }

        public static Func<TokenStream, IParseResult<T>> InCompleteError<T>
            (this Func<TokenStream, IParseResult<T>> parser, string errorMessage, Position position, ErrorKind errorKind = ErrorKind.None)
        {
            return parser.InCompleteError(stream =>
               new Error(errorMessage, errorKind, position)
           );
        }

        public static Func<TokenStream, IParseResult<T>> InCompleteErrorWithPositionEnd<T>
            (this Func<TokenStream, IParseResult<T>> parser, string errorMessage, ErrorKind errorKind = ErrorKind.None)
        {
            return parser.InCompleteError(stream =>
                new Error(errorMessage, errorKind, stream.LastCurrent.PositionEnd)
            );
        }

        public static Func<TokenStream, IParseResult<T>> InCompleteErrorWithPositionHead<T>
            (this Func<TokenStream, IParseResult<T>> parser, string errorMessage, ErrorKind errorKind = ErrorKind.None)
        {
            return parser.InCompleteError(stream =>
                new Error(errorMessage, errorKind, stream.LastCurrent.position)
            );
        }

        public static Func<TokenStream, IParseResult<TT>> Right<T, TT>
           (this Func<TokenStream, IParseResult<T>> parser, Func<TokenStream, IParseResult<TT>> parser2)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return result.CastError<TT>();
                if (stream.IsEnd)
                    return ParseResult<TT>.CreateError(new Error("", ErrorKind.InComplete));
                return parser2(stream);
            };
        }

        public static Func<TokenStream, IParseResult<T>> Left<T, TT>
           (this Func<TokenStream, IParseResult<T>> parser, Func<TokenStream, IParseResult<TT>> parser2)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError == false)
                {
                    if (stream.IsEnd)
                        return ParseResult<T>.CreateError(new Error("", ErrorKind.InComplete));
                    var result2 = parser2(stream);
                    if (result2.IsError)
                        return result2.CastError<T>();
                }
                return result;
            };
        }

        public static Func<TokenStream, IParseResult<TT>> Bind<T, TT>
           (this Func<TokenStream, IParseResult<T>> parser, Func<T, Func<TokenStream, IParseResult<TT>>> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return result.CastError<TT>();
                if (stream.IsEnd)
                    return ParseResult<TT>.CreateError(new Error("", ErrorKind.InComplete));
                return func(result.Value)(stream);
            };
        }

        public static Func<TokenStream, IParseResult<TT>> BindResult<T, TT>
            (this Func<TokenStream, IParseResult<T>> parser, Func<T, IParseResult<TT>> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return result.CastError<TT>();
                return func(result.Value);
            };
        }

        public static Func<TokenStream, IParseResult<TT>> MapResult<T, TT>
          (this Func<TokenStream, IParseResult<T>> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => ParseResult<TT>.CreateSuccess(func(t)));
        }
    }
}
