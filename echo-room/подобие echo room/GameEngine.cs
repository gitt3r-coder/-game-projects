using System.Collections.Generic;
using System.Linq;

namespace подобие_echo_room
{
    public class GameEngine
    {
        public const int Rows = 9;
        public const int Columns = 13;
        public const double EchoDelay = 10.0;
        private const int StartRow = 7;
        private const int StartColumn = 1;

        private readonly HashSet<string> walls;
        private double nextEchoDelay;

        public Actor Player { get; private set; }
        public List<EchoActor> Echoes { get; private set; }
        public List<GameAction> Actions { get; private set; }
        public List<RoomObject> Objects { get; private set; }
        public ConsoleObject PastConsole { get; private set; }
        public ConsoleObject PresentConsole { get; private set; }
        public DoorObject Door { get; private set; }
        public ExitObject Exit { get; private set; }
        public double CurrentTime { get; private set; }
        public string LastMessage { get; set; }
        public bool IsWin { get; private set; }

        public GameEngine()
        {
            walls = new HashSet<string>();
            Echoes = new List<EchoActor>();
            Actions = new List<GameAction>();
            Objects = new List<RoomObject>();
            Restart();
        }

        public void Restart()
        {
            walls.Clear();
            Objects.Clear();
            Echoes.Clear();
            Actions.Clear();

            CurrentTime = 0;
            nextEchoDelay = EchoDelay;
            IsWin = false;
            LastMessage = "Нажмите WASD/стрелки для движения, E для действия.";

            Player = new Actor("Игрок", StartRow, StartColumn, false);

            for (int row = 0; row < Rows; row++)
            {
                AddWall(row, 0);
                AddWall(row, Columns - 1);
            }

            for (int column = 0; column < Columns; column++)
            {
                AddWall(0, column);
                AddWall(Rows - 1, column);
            }

            for (int column = 1; column < Columns - 1; column++)
            {
                if (column != 6)
                    AddWall(4, column);
            }

            AddWall(2, 3);
            AddWall(2, 4);
            AddWall(6, 5);
            AddWall(6, 6);
            AddWall(6, 7);

            PastConsole = new ConsoleObject("Пульт прошлого", 6, 2, ConsoleOwner.EchoOnly, 8.0);
            PresentConsole = new ConsoleObject("Пульт настоящего", 6, 10, ConsoleOwner.PlayerOnly, 8.0);
            Door = new DoorObject("Дверь памяти", 4, 6);
            Exit = new ExitObject("Выход", 1, 11);

            Objects.Add(PastConsole);
            Objects.Add(PresentConsole);
            Objects.Add(Door);
            Objects.Add(Exit);
        }

        public void Update(double seconds)
        {
            if (IsWin)
                return;

            CurrentTime += seconds;

            if (CurrentTime >= nextEchoDelay && Echoes.Count < 3)
            {
                Echoes.Add(new EchoActor("Эхо " + (Echoes.Count + 1), StartRow, StartColumn, nextEchoDelay));
                nextEchoDelay += EchoDelay;
                LastMessage = "В комнате появилась новая копия прошлого.";
            }

            foreach (EchoActor echo in Echoes.ToList())
            {
                echo.Update(this, Actions);
            }

            if (Player.Row == Exit.Row && Player.Column == Exit.Column)
            {
                IsWin = true;
                LastMessage = "Вы выбрались. Комната наконец замолчала.";
            }
        }

        public void MovePlayer(Direction direction)
        {
            if (IsWin)
                return;

            if (TryMove(Player, direction))
            {
                Actions.Add(new GameAction(CurrentTime, GameActionType.Move, direction));
            }
        }

        public void InteractPlayer()
        {
            if (IsWin)
                return;

            Actions.Add(new GameAction(CurrentTime, GameActionType.Interact, Direction.None));
            InteractNear(Player);
        }

        public void PerformEchoAction(EchoActor echo, GameAction action)
        {
            if (action.Type == GameActionType.Move)
            {
                TryMove(echo, action.Direction);
            }
            else
            {
                InteractNear(echo);
            }
        }

        public bool IsWall(int row, int column)
        {
            return walls.Contains(GetKey(row, column));
        }

        public bool IsDoorCell(int row, int column)
        {
            return Door.Row == row && Door.Column == column;
        }

        public bool IsBlocked(int row, int column)
        {
            if (row < 0 || row >= Rows || column < 0 || column >= Columns)
                return true;

            if (IsWall(row, column))
                return true;

            if (IsDoorCell(row, column) && !Door.IsOpen(this))
                return true;

            return false;
        }

        private bool TryMove(Actor actor, Direction direction)
        {
            int row = actor.Row;
            int column = actor.Column;

            if (direction == Direction.Up)
                row--;
            if (direction == Direction.Down)
                row++;
            if (direction == Direction.Left)
                column--;
            if (direction == Direction.Right)
                column++;

            if (IsBlocked(row, column))
            {
                if (!actor.IsEcho)
                    LastMessage = "Проход закрыт. Нужны прошлое и настоящее одновременно.";

                return false;
            }

            actor.Row = row;
            actor.Column = column;
            return true;
        }

        private void InteractNear(Actor actor)
        {
            RoomObject found = Objects
                .Where(o => !(o is DoorObject) && !(o is ExitObject))
                .FirstOrDefault(o => GetDistance(actor.Row, actor.Column, o.Row, o.Column) <= 1);

            if (found != null)
            {
                found.Interact(actor, this);
            }
            else if (!actor.IsEcho)
            {
                LastMessage = "Рядом нет объекта для действия.";
            }
        }

        private int GetDistance(int row1, int column1, int row2, int column2)
        {
            int row = row1 - row2;
            int column = column1 - column2;

            if (row < 0)
                row = -row;

            if (column < 0)
                column = -column;

            return row + column;
        }

        private void AddWall(int row, int column)
        {
            walls.Add(GetKey(row, column));
        }

        private string GetKey(int row, int column)
        {
            return row + ":" + column;
        }
    }
}
