namespace MarineLang
{
    public class Stream<T>
    {
        readonly T[] items;
        public int Index { get; private set; } = -1;
        public bool IsEnd { get; private set; } = false;

        public T Current => items[Index];

        public Stream(T[] items)
        {
            this.items = items;
        }

        public bool MoveNext()
        {
            Index++;
            if (items.Length > Index)
                return true;
            IsEnd = true;
            return false;
        }

        public void SetIndex(int index)
        {
            Index = index;
            if (items.Length <= Index)
                IsEnd = true;
        }
    }
}
