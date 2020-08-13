using MarineLang.Models;
using MarineLang.Streams;
using System;

namespace MarineLang.SyntaxAnalysis
{


    public static class ParserCombinatorExtension
    {
        public static Parser<T> InCompleteError<T>(this Parser<T> parser, Func<TokenStream, Error> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError && result.Error.ErrorKind == ErrorKind.InComplete)
                    return ParseResult<T>.CreateError(func(stream));
                return result;
            };
        }

        public static Parser<T> InCompleteError<T>
            (this Parser<T> parser, string errorMessage, ErrorCode errorCode, Position position, ErrorKind errorKind = ErrorKind.None)
        {
            return parser.InCompleteError(stream =>
               new Error(errorMessage, errorCode, errorKind, position)
           );
        }

        public static Parser<T> InCompleteErrorWithPositionEnd<T>
            (this Parser<T> parser, string errorMessage, ErrorCode errorCode, ErrorKind errorKind = ErrorKind.None)
        {
            return parser.InCompleteError(stream =>
                new Error(errorMessage, errorCode, errorKind, stream.LastCurrent.PositionEnd)
            );
        }

        public static Parser<T> InCompleteErrorWithPositionHead<T>
            (this Parser<T> parser, string errorMessage, ErrorCode errorCode, ErrorKind errorKind = ErrorKind.None)
        {
            return parser.InCompleteError(stream =>
                new Error(errorMessage, errorCode, errorKind, stream.LastCurrent.position)
            );
        }

        public static Parser<TT> Right<T, TT>(this Parser<T> parser, Parser<TT> parser2)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return result.CastError<TT>();
                if (stream.IsEnd)
                    return ParseResult<TT>.CreateError(new Error(ErrorKind.InComplete));
                return parser2(stream);
            };
        }

        public static Parser<T> Left<T, TT>(this Parser<T> parser, Parser<TT> parser2)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError == false)
                {
                    if (stream.IsEnd)
                        return ParseResult<T>.CreateError(new Error(ErrorKind.InComplete));
                    var result2 = parser2(stream);
                    if (result2.IsError)
                        return result2.CastError<T>();
                }
                return result;
            };
        }

        public static Parser<T> ExpectCanMoveNext<T>(this Parser<T> parser)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError == false && stream.IsEnd)
                    return ParseResult<T>.CreateError(new Error(ErrorKind.InComplete));
                return result;
            };
        }

        public static Parser<TT> Bind<T, TT>(this Parser<T> parser, Func<T, Parser<TT>> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return result.CastError<TT>();
                if (stream.IsEnd)
                    return ParseResult<TT>.CreateError(new Error(ErrorKind.InComplete));
                return func(result.Value)(stream);
            };
        }

        public static Parser<TT> BindResult<T, TT>(this Parser<T> parser, Func<T, IParseResult<TT>> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return result.CastError<TT>();
                return func(result.Value);
            };
        }

        public static Parser<TT> MapResult<T, TT>(this Parser<T> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => ParseResult<TT>.CreateSuccess(func(t)));
        }
    }
}
