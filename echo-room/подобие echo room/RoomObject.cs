namespace подобие_echo_room
{
    public enum ConsoleOwner
    {
        Any,
        PlayerOnly,
        EchoOnly
    }

    public abstract class RoomObject
    {
        public string Name { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }

        protected RoomObject(string name, int row, int column)
        {
            Name = name;
            Row = row;
            Column = column;
        }

        public virtual bool IsActive(GameEngine game)
        {
            return false;
        }

        public virtual void Interact(Actor actor, GameEngine game)
        {
        }
    }

    public class ConsoleObject : RoomObject
    {
        private readonly ConsoleOwner owner;
        private readonly double activeSeconds;
        private double activeUntil;

        public ConsoleOwner Owner
        {
            get { return owner; }
        }

        public ConsoleObject(string name, int row, int column, ConsoleOwner owner, double activeSeconds)
            : base(name, row, column)
        {
            this.owner = owner;
            this.activeSeconds = activeSeconds;
            activeUntil = -1;
        }

        public override bool IsActive(GameEngine game)
        {
            return game.CurrentTime <= activeUntil;
        }

        public override void Interact(Actor actor, GameEngine game)
        {
            bool canUse = owner == ConsoleOwner.Any ||
                          (owner == ConsoleOwner.PlayerOnly && !actor.IsEcho) ||
                          (owner == ConsoleOwner.EchoOnly && actor.IsEcho);

            if (canUse)
            {
                activeUntil = game.CurrentTime + activeSeconds;
                game.LastMessage = actor.Name + " активировал: " + Name;
            }
            else if (owner == ConsoleOwner.EchoOnly)
            {
                game.LastMessage = "Пульт прошлого не сработал. Он слушает только эхо.";
            }
            else
            {
                game.LastMessage = "Пульт настоящего реагирует только на живого игрока.";
            }
        }
    }

    public class DoorObject : RoomObject
    {
        public DoorObject(string name, int row, int column)
            : base(name, row, column)
        {
        }

        public bool IsOpen(GameEngine game)
        {
            return game.PastConsole.IsActive(game) && game.PresentConsole.IsActive(game);
        }
    }

    public class ExitObject : RoomObject
    {
        public ExitObject(string name, int row, int column)
            : base(name, row, column)
        {
        }
    }
}
