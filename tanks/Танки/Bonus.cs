namespace Tanki
{
    public enum BonusType
    {
        Star,
        Shield,
        Grenade,
        Life,
        Freeze,
        Fortify
    }

    public class Bonus
    {
        public double X { get; set; }
        public double Y { get; set; }
        public BonusType Type { get; set; }
        public bool IsAlive { get; set; }
        public int Lifetime { get; set; }
        public bool Visible { get; set; }
        public int BlinkTimer { get; set; }

        public Bonus(double x, double y, BonusType type)
        {
            X = x;
            Y = y;
            Type = type;
            IsAlive = true;
            Lifetime = 1200;
            Visible = true;
            BlinkTimer = 0;
        }
    }
}