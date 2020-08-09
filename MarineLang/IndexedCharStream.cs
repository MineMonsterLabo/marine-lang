using System.Linq;

namespace MarineLang
{
    public struct IndexedChar
    {
        public int index;
        public char c;
    }

    public class IndexedCharStream : Stream<IndexedChar>
    {
        public static IndexedCharStream Create(string str)
        {
            var indexedChars = Enumerable.Range(0, str.Length).Select(index => new IndexedChar { index = index, c = str[index] });
            return new IndexedCharStream(indexedChars.ToArray());
        }

        IndexedCharStream(IndexedChar[] indexedChars) : base(indexedChars)
        {
        }
    }
}
