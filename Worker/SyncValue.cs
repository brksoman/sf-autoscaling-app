using System.Threading;

namespace Worker
{
    public class SyncValue
    {
        private readonly int minValue;
        private readonly int maxValue;

        private int value;

        public SyncValue(int minValue, int maxValue, int value)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.value = value;
        }

        public int Value
        {
            get => value;
            set => Interlocked.Exchange(ref this.value,
                (value <= minValue) ? minValue :
                (value >= maxValue) ? maxValue : value);
        }
    }
}
