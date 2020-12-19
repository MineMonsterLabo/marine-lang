using System.Collections.Generic;
using System.Linq;

namespace MarineLang.VirtualMachines
{
    public class MarineValue
    {
        public object Value { get; }
        public bool IsCoroutine { get; } = false;
        public IEnumerable<object> Coroutine => coroutine;

        readonly IEnumerable<object> coroutine;

        public MarineValue(object value)
        {
            Value = value;
        }

        public MarineValue(IEnumerable<object> coroutine)
        {
            this.coroutine = coroutine;
            IsCoroutine = true;
        }

        public object Eval()
        {
            return coroutine == null ? Value : coroutine.Last();
        }
    }

    public class MarineValue<T>
    {
        public T Value { get; }
        public bool IsCoroutine { get; }
        public IEnumerable<T> Coroutine => coroutine;

        readonly IEnumerable<T> coroutine;

        public MarineValue(MarineValue marineValue)
        {
            if (marineValue.IsCoroutine)
                coroutine = marineValue.Coroutine.Select(x => x == null ? default(T) : (T)x);
            else
                Value = (T)marineValue.Value;

            IsCoroutine = marineValue.IsCoroutine;
        }

        public T Eval()
        {
            return coroutine == null ? Value : coroutine.Last();
        }
    }
}
