using MarineLang.Models;
using MarineLang.Streams;
using System;

namespace MarineLang.SyntaxAnalysis
{
    public static class ParserCombinatorExtension
    {
        public static Func<TokenStream, IParseResult<T>> Error<T>
            (this Func<TokenStream, IParseResult<T>> parser, string errorMessage, Position position = default)
        {
            return stream =>
            {
                var result = parser(stream);
                if (result.IsError)
                    return ParseResult<T>.Error(errorMessage, position);
                return result;
            };
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
                    return ParseResult<TT>.Error("末尾です");
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
                        return ParseResult<T>.Error("末尾です");
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
                    return ParseResult<TT>.Error("末尾です");
                return func(result.Value)(stream);
            };
        }

        public static Func<TokenStream, IParseResult<TT>> BindResult<T, TT>
                 (this Func<TokenStream, IParseResult<T>> parser, Func<T, IParseResult<TT>> func)
        {
            return parser.Bind<T, TT>(t => stream => func(t));
        }

        public static Func<TokenStream, IParseResult<TT>> MapResult<T, TT>
          (this Func<TokenStream, IParseResult<T>> parser, Func<T, TT> func)
        {
            return parser.BindResult(t => ParseResult<TT>.Success(func(t)));
        }
    }
}
