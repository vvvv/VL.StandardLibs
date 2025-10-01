namespace VL.Lib.Application
{
    public class TimedValue<T>
    {
        public TimedValue(double from, double to, T value)
        {
            From = from;
            To = to;
            Value = value;
        }

        public double From;
        public double To;
        public T Value;

        public void Split(out double from, out double to, out T value)
        {
            from = From;
            to = To;
            value = Value;
        }
    }
}
