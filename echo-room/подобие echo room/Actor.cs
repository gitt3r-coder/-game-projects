namespace подобие_echo_room
{
    public class Actor
    {
        public string Name { get; private set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsEcho { get; private set; }

        public Actor(string name, int row, int column, bool isEcho)
        {
            Name = name;
            Row = row;
            Column = column;
            IsEcho = isEcho;
        }
    }
}
