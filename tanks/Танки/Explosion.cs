namespace Tanki
{
    public class Explosion
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Timer { get; set; }
        public int MaxTimer { get; set; }
        public bool Big { get; set; }
        public bool IsAlive { get; set; }

        public Explosion(double x, double y, bool big)
        {
            X = x;
            Y = y;
            Big = big;
            Timer = 0;
            MaxTimer = big ? 18 : 10;
            IsAlive = true;
        }

        public void Update()
        {
            Timer++;
            if (Timer >= MaxTimer) IsAlive = false;
        }
    }
}