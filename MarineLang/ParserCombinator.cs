using System;
using System.Collections.Generic;

namespace MarineLang
{
    public static class ParserCombinator
    {
        public static Func<TokenStream, ParseResult<IEnumerable<T>>> Many<T>(Func<TokenStream, ParseResult<T>> parser)
        {
            return
                stream =>
                {
                    var list = new List<T>();
                    while (stream.IsEnd == false)
                    {
                        var parseResult = parser(stream);

                        if (parseResult.isError)
                            return ParseResult<IEnumerable<T>>.Error("");

                        list.Add(parseResult.value);
                    }
                    return ParseResult<IEnumerable<T>>.Success(list);
                };
        }

        public static Func<TokenStream, ParseResult<T>> Try<T>(Func<TokenStream, ParseResult<T>> parser)
        {
            return
                stream =>
                {
                    var backUpIndex = stream.Index;
                    var parseResult = parser(stream);

                    if (parseResult.isError)
                        stream.SetIndex(backUpIndex);

                    return parseResult;
                };
        }
    }
}
