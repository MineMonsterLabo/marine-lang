using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.Streams;
using MineUtil;
using System;

namespace MarineLang.SyntaxAnalysis
{


    public static class ParserExtension
    {
        public static Parser<T> InCompleteError<T>(this Parser<T> parser, Func<TokenStream, ParseErrorInfo> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError && result.RawError.ErrorKind == ErrorKind.InComplete)
                    return ParseResult.Error<T>(func(stream));
                return result;
            };
        }

        public static Parser<T> InCompleteError<T>
            (this Parser<T> parser, ErrorCode errorCode, RangePosition rangePosition, ErrorKind errorKind = ErrorKind.None, string prefixErrorMessage = "")
        {
            return parser.InCompleteError(stream =>
               new ParseErrorInfo(prefixErrorMessage, errorCode, errorKind, rangePosition)
           );
        }

        public static Parser<T> InCompleteErrorWithPositionEnd<T>
            (this Parser<T> parser, ErrorCode errorCode, ErrorKind errorKind = ErrorKind.None, string prefixErrorMessage = "")
        {
            return parser.InCompleteError(stream =>
                new ParseErrorInfo(prefixErrorMessage, errorCode, errorKind, stream.LastCurrent.rangePosition)
            );
        }

        public static Parser<T> InCompleteErrorWithPositionHead<T>
            (this Parser<T> parser, ErrorCode errorCode, ErrorKind errorKind = ErrorKind.None, string prefixErrorMessage = "")
        {
            return parser.InCompleteError(stream =>
                new ParseErrorInfo(prefixErrorMessage, errorCode, errorKind, stream.LastCurrent.rangePosition)
            );
        }

        public static Parser<TT> Right<T, TT>(this Parser<T> parser, Parser<TT> parser2)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return ParseResult.Error<TT>(result.RawError);
                if (stream.IsEnd)
                    return ParseResult.Error<TT>(new ParseErrorInfo(ErrorKind.InComplete));
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
                        return ParseResult.Error<T>(new ParseErrorInfo(ErrorKind.InComplete));
                    var result2 = parser2(stream);
                    if (result2.IsError)
                        return ParseResult.Error<T>(result2.RawError);
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
                    return ParseResult.Error<T>(new ParseErrorInfo(ErrorKind.InComplete));
                return result;
            };
        }

        public static Parser<TT> Bind<T, TT>(this Parser<T> parser, Func<T, Parser<TT>> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return ParseResult.Error<TT>(result.RawError);
                return func(result.RawValue)(stream);
            };
        }

        public static Parser<V> SelectMany<T, U, V>(
                 this Parser<T> parser,
                 Func<T, Parser<U>> selector,
                 Func<T, U, V> projector)
        {
            return parser.Bind(t => selector(t).Select(u => projector(t, u)));
        }

        public static Parser<TT> BindResult<T, TT>(this Parser<T> parser, Func<T, IResult<TT, ParseErrorInfo>> func)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return ParseResult.Error<TT>(result.RawError);
                return func(result.RawValue);
            };
        }

        public static Parser<TT> MapResult<T, TT>(this Parser<T> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => ParseResult.Ok(func(t)));
        }

        public static Parser<TT> Select<T, TT>(this Parser<T> parser, Func<T, TT> selector)
        {
            return MapResult<T, TT>(parser, selector);
        }

        public static Parser<T> Try<T>(this Parser<T> parser)
        {
            return
                stream =>
                {
                    var backUpIndex = stream.Index;
                    var parseResult = parser(stream);

                    if (parseResult.IsError)
                        stream.SetIndex(backUpIndex);

                    return parseResult;
                };
        }

        public static Parser<T> Default<T>(this Parser<T> parser, T defaultValue)
        {
            return stream =>
            {
                var parseResult = parser(stream);
                if (parseResult.IsError)
                {
                    return ParseResult.Ok(defaultValue);
                }
                return parseResult;
            };
        }
    }
}
