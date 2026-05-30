namespace Tanki
{
    public class Coin
    {
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsAlive { get; set; }
        public int Value { get; set; }
        public int AnimFrame { get; set; }
        public int AnimTimer { get; set; }
        public int SparkleTimer { get; set; }

        public Coin(double x, double y, int value = 1)
        {
            X = x;
            Y = y;
            Value = value;
            IsAlive = true;
            AnimFrame = 0;
            AnimTimer = 0;
            SparkleTimer = 0;
        }
    }
}