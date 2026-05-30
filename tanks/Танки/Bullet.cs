namespace Tanki
{
    public class Bullet
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Direction Direction { get; set; }
        public double Speed { get; set; }
        public int Damage { get; set; }
        public int OwnerTeam { get; set; }
        public bool IsActive { get; set; } = true;

        public Bullet(double x, double y, Direction dir, double speed, int damage, int ownerTeam)
        {
            X = x; Y = y; Direction = dir; Speed = speed; Damage = damage; OwnerTeam = ownerTeam;
        }

        public void Update()
        {
            if (Direction == Direction.Up) Y -= Speed;
            else if (Direction == Direction.Down) Y += Speed;
            else if (Direction == Direction.Left) X -= Speed;
            else if (Direction == Direction.Right) X += Speed;
        }
    }
}