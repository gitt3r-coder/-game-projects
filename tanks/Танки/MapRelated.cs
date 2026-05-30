namespace Tanki
{
    public enum CellType
    {
        Empty,
        Brick,
        Concrete,
        Water,
        Bush,
        Ice,
        HeadquartersTop,
        HeadquartersBottom
    }

    public class MapCell
    {
        public CellType Type { get; set; }
        public int Health { get; set; }

        public MapCell(CellType type)
        {
            Type = type;
            Health = type == CellType.Brick ? 1 : 100;
        }
    }
}
